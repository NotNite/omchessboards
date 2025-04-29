use crate::protos::network::{ServerMovesAndCaptures, ServerStateSnapshot};
use parking_lot::Mutex;
use std::cmp::Ordering;
use std::collections::VecDeque;
use std::ops::Rem;
use std::sync::atomic;
use std::sync::atomic::AtomicI64;
use rand::seq::SliceRandom;
use tokio::time::Instant;
use tracing::info;

const BOARD_SIZE: (u32, u32) = (8000, 8000);
const GRID_SIZE: u32 = 48 * 2; // the size of the grid we get on each fetch

#[derive(Debug, Copy, Clone)]
struct Square {}

#[derive(Debug, Copy, Clone)]
struct GridEntry {
    center_x: u32,
    center_y: u32,
    last_scraped_at: Option<Instant>,
}

impl GridEntry {
    pub fn new(center_x: u32, center_y: u32, last_scraped_at: Option<Instant>) -> Self {
        assert_eq!(center_x.rem(GRID_SIZE), 0, "center x isn't divisible by grid size");
        assert_eq!(center_y.rem(GRID_SIZE), 0, "center y isn't divisible by grid size");

        Self {
            center_x,
            center_y,
            last_scraped_at,
        }
    }
}

impl Eq for GridEntry {}

impl PartialEq<Self> for GridEntry {
    fn eq(&self, other: &Self) -> bool {
        self.center_x == other.center_x && self.center_y == other.center_y
    }
}

impl PartialOrd<Self> for GridEntry {
    fn partial_cmp(&self, other: &Self) -> Option<Ordering> {
        Some(self.cmp(other))
    }
}

impl Ord for GridEntry {
    fn cmp(&self, other: &Self) -> Ordering {
        if self.center_x == other.center_x && self.center_y == other.center_y {
            Ordering::Equal
        } else {
            match (&self.last_scraped_at, &other.last_scraped_at) {
                (None, Some(_)) => Ordering::Greater,
                (Some(_), None) => Ordering::Less,
                (None, None) => Ordering::Equal,

                // if `other_time > self_time` then `self` should be handled first
                (Some(self_time), Some(other_time)) => other_time.cmp(self_time)
            }
        }
    }
}

/// Stores the state of the board and generates tasks for workers.
pub struct BoardState {
    squares: Box<[Square]>,
    queue: Mutex<VecDeque<GridEntry>>,
    counter: AtomicI64,
}

impl BoardState {
    pub fn new() -> BoardState {
        let squares = vec![Square {}; BOARD_SIZE.0 as usize * BOARD_SIZE.1 as usize];
        let squares = squares.into_boxed_slice();

        let mut queue = Vec::new();

        for grid_x in 0..BOARD_SIZE.0.div_ceil(GRID_SIZE) {
            for grid_y in 0..BOARD_SIZE.1.div_ceil(GRID_SIZE) {
                let center_x = grid_x * GRID_SIZE;
                let center_y = grid_y * GRID_SIZE;
                queue.push(GridEntry::new(center_x, center_y, None))
            }
        }

        queue.shuffle(&mut rand::rng());

        BoardState {
            squares,
            queue: Mutex::new(VecDeque::from(queue)),
            counter: AtomicI64::new(0),
        }
    }

    /// Pops and returns the current highest priority task.
    pub async fn get_task(&self) -> Task<'_> {
        let mut guard = self.queue.lock();
        let entry = guard.pop_front();
        drop(guard);

        let entry = entry.expect("too many scraping tasks (out of entries)");

        Task {
            entry,
            should_return: true,
            board: self,
        }
    }

    /// Returns a task at the given position.
    pub fn task_at(&self, center_x: u32, center_y: u32) -> Task<'_> {
        let entry = GridEntry::new(center_x, center_y, None);

        Task {
            entry,
            should_return: false,
            board: self
        }
    }

    /// Handles a moves and updates payload.
    pub fn handle_moves_and_captures(&self, _moves_and_captures: ServerMovesAndCaptures) {
        // TODO
    }

    /// Handles a state snapshot.
    pub fn handle_snapshot(&self, snapshot: ServerStateSnapshot) {
        let count = self.counter.fetch_add(snapshot.pieces.len() as i64, atomic::Ordering::Relaxed);
        info!("scraped {} total pieces", count);
    }

    fn finish_task(&self, task: &Task<'_>) {
        if !task.should_return {
            return;
        }

        let mut guard = self.queue.lock();
        guard.push_back(task.entry);
        drop(guard);
    }
}

/// A task from a [`BoardState`] for a worker to complete.
pub struct Task<'s> {
    entry: GridEntry,
    should_return: bool,
    board: &'s BoardState
}

impl Drop for Task<'_> {
    fn drop(&mut self) {
        if self.should_return {
            self.board.finish_task(self)
        }
    }
}

impl Task<'_> {
    pub fn center_x(&self) -> u32 {
        self.entry.center_x
    }

    pub fn center_y(&self) -> u32 {
        self.entry.center_y
    }

    /// Finishes this task and returns it to the board.
    pub fn finish(mut self) {
        self.entry.last_scraped_at = Some(Instant::now());
        drop(self);
    }

    /// Abandons this task, placing it back into queue at its original position.
    pub fn abandon(self) {
        drop(self)
    }
}