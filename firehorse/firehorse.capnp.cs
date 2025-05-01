using Capnp;
using Capnp.Rpc;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CapnpGen
{
    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0x84d6dc2582ae11ebUL)]
    public enum PieceType : ushort
    {
        pawn,
        knight,
        bishop,
        rook,
        queen,
        king,
        promotedPawn
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xbce2074586e66bb4UL)]
    public enum MoveType : ushort
    {
        normal,
        castle,
        enPassant
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xefa56ea98d1ea1cdUL)]
    public class PieceData : ICapnpSerializable
    {
        public const UInt64 typeId = 0xefa56ea98d1ea1cdUL;
        void ICapnpSerializable.Deserialize(DeserializerState arg_)
        {
            var reader = READER.create(arg_);
            Id = reader.Id;
            Type = reader.Type;
            IsWhite = reader.IsWhite;
            MoveCount = reader.MoveCount;
            CaptureCount = reader.CaptureCount;
            applyDefaults();
        }

        public void serialize(WRITER writer)
        {
            writer.Id = Id;
            writer.Type = Type;
            writer.IsWhite = IsWhite;
            writer.MoveCount = MoveCount;
            writer.CaptureCount = CaptureCount;
        }

        void ICapnpSerializable.Serialize(SerializerState arg_)
        {
            serialize(arg_.Rewrap<WRITER>());
        }

        public void applyDefaults()
        {
        }

        public uint Id
        {
            get;
            set;
        }

        public CapnpGen.PieceType Type
        {
            get;
            set;
        }

        public bool IsWhite
        {
            get;
            set;
        }

        public uint MoveCount
        {
            get;
            set;
        }

        public uint CaptureCount
        {
            get;
            set;
        }

        public struct READER
        {
            readonly DeserializerState ctx;
            public READER(DeserializerState ctx)
            {
                this.ctx = ctx;
            }

            public static READER create(DeserializerState ctx) => new READER(ctx);
            public static implicit operator DeserializerState(READER reader) => reader.ctx;
            public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            public uint Id => ctx.ReadDataUInt(0UL, 0U);
            public CapnpGen.PieceType Type => (CapnpGen.PieceType)ctx.ReadDataUShort(32UL, (ushort)0);
            public bool IsWhite => ctx.ReadDataBool(48UL, false);
            public uint MoveCount => ctx.ReadDataUInt(64UL, 0U);
            public uint CaptureCount => ctx.ReadDataUInt(96UL, 0U);
        }

        public class WRITER : SerializerState
        {
            public WRITER()
            {
                this.SetStruct(2, 0);
            }

            public uint Id
            {
                get => this.ReadDataUInt(0UL, 0U);
                set => this.WriteData(0UL, value, 0U);
            }

            public CapnpGen.PieceType Type
            {
                get => (CapnpGen.PieceType)this.ReadDataUShort(32UL, (ushort)0);
                set => this.WriteData(32UL, (ushort)value, (ushort)0);
            }

            public bool IsWhite
            {
                get => this.ReadDataBool(48UL, false);
                set => this.WriteData(48UL, value, false);
            }

            public uint MoveCount
            {
                get => this.ReadDataUInt(64UL, 0U);
                set => this.WriteData(64UL, value, 0U);
            }

            public uint CaptureCount
            {
                get => this.ReadDataUInt(96UL, 0U);
                set => this.WriteData(96UL, value, 0U);
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xc07a3d23a2954206UL)]
    public class SnapshotPiece : ICapnpSerializable
    {
        public const UInt64 typeId = 0xc07a3d23a2954206UL;
        void ICapnpSerializable.Deserialize(DeserializerState arg_)
        {
            var reader = READER.create(arg_);
            Dx = reader.Dx;
            Dy = reader.Dy;
            Data = CapnpSerializable.Create<CapnpGen.PieceData>(reader.Data);
            applyDefaults();
        }

        public void serialize(WRITER writer)
        {
            writer.Dx = Dx;
            writer.Dy = Dy;
            Data?.serialize(writer.Data);
        }

        void ICapnpSerializable.Serialize(SerializerState arg_)
        {
            serialize(arg_.Rewrap<WRITER>());
        }

        public void applyDefaults()
        {
        }

        public sbyte Dx
        {
            get;
            set;
        }

        public sbyte Dy
        {
            get;
            set;
        }

        public CapnpGen.PieceData Data
        {
            get;
            set;
        }

        public struct READER
        {
            readonly DeserializerState ctx;
            public READER(DeserializerState ctx)
            {
                this.ctx = ctx;
            }

            public static READER create(DeserializerState ctx) => new READER(ctx);
            public static implicit operator DeserializerState(READER reader) => reader.ctx;
            public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            public sbyte Dx => ctx.ReadDataSByte(0UL, (sbyte)0);
            public sbyte Dy => ctx.ReadDataSByte(8UL, (sbyte)0);
            public CapnpGen.PieceData.READER Data => ctx.ReadStruct(0, CapnpGen.PieceData.READER.create);
        }

        public class WRITER : SerializerState
        {
            public WRITER()
            {
                this.SetStruct(1, 1);
            }

            public sbyte Dx
            {
                get => this.ReadDataSByte(0UL, (sbyte)0);
                set => this.WriteData(0UL, value, (sbyte)0);
            }

            public sbyte Dy
            {
                get => this.ReadDataSByte(8UL, (sbyte)0);
                set => this.WriteData(8UL, value, (sbyte)0);
            }

            public CapnpGen.PieceData.WRITER Data
            {
                get => BuildPointer<CapnpGen.PieceData.WRITER>(0);
                set => Link(0, value);
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xb54ae173b446e119UL)]
    public class Snapshot : ICapnpSerializable
    {
        public const UInt64 typeId = 0xb54ae173b446e119UL;
        void ICapnpSerializable.Deserialize(DeserializerState arg_)
        {
            var reader = READER.create(arg_);
            X = reader.X;
            Y = reader.Y;
            Pieces = reader.Pieces?.ToReadOnlyList(_ => CapnpSerializable.Create<CapnpGen.SnapshotPiece>(_));
            applyDefaults();
        }

        public void serialize(WRITER writer)
        {
            writer.X = X;
            writer.Y = Y;
            writer.Pieces.Init(Pieces, (_s1, _v1) => _v1?.serialize(_s1));
        }

        void ICapnpSerializable.Serialize(SerializerState arg_)
        {
            serialize(arg_.Rewrap<WRITER>());
        }

        public void applyDefaults()
        {
        }

        public ushort X
        {
            get;
            set;
        }

        public ushort Y
        {
            get;
            set;
        }

        public IReadOnlyList<CapnpGen.SnapshotPiece> Pieces
        {
            get;
            set;
        }

        public struct READER
        {
            readonly DeserializerState ctx;
            public READER(DeserializerState ctx)
            {
                this.ctx = ctx;
            }

            public static READER create(DeserializerState ctx) => new READER(ctx);
            public static implicit operator DeserializerState(READER reader) => reader.ctx;
            public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            public ushort X => ctx.ReadDataUShort(0UL, (ushort)0);
            public ushort Y => ctx.ReadDataUShort(16UL, (ushort)0);
            public IReadOnlyList<CapnpGen.SnapshotPiece.READER> Pieces => ctx.ReadList(0).Cast(CapnpGen.SnapshotPiece.READER.create);
        }

        public class WRITER : SerializerState
        {
            public WRITER()
            {
                this.SetStruct(1, 1);
            }

            public ushort X
            {
                get => this.ReadDataUShort(0UL, (ushort)0);
                set => this.WriteData(0UL, value, (ushort)0);
            }

            public ushort Y
            {
                get => this.ReadDataUShort(16UL, (ushort)0);
                set => this.WriteData(16UL, value, (ushort)0);
            }

            public ListOfStructsSerializer<CapnpGen.SnapshotPiece.WRITER> Pieces
            {
                get => BuildPointer<ListOfStructsSerializer<CapnpGen.SnapshotPiece.WRITER>>(0);
                set => Link(0, value);
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xfcc17d2a03f5203eUL)]
    public class Move : ICapnpSerializable
    {
        public const UInt64 typeId = 0xfcc17d2a03f5203eUL;
        void ICapnpSerializable.Deserialize(DeserializerState arg_)
        {
            var reader = READER.create(arg_);
            Id = reader.Id;
            FromX = reader.FromX;
            FromY = reader.FromY;
            ToX = reader.ToX;
            ToY = reader.ToY;
            MoveType = reader.MoveType;
            PieceIsWhite = reader.PieceIsWhite;
            applyDefaults();
        }

        public void serialize(WRITER writer)
        {
            writer.Id = Id;
            writer.FromX = FromX;
            writer.FromY = FromY;
            writer.ToX = ToX;
            writer.ToY = ToY;
            writer.MoveType = MoveType;
            writer.PieceIsWhite = PieceIsWhite;
        }

        void ICapnpSerializable.Serialize(SerializerState arg_)
        {
            serialize(arg_.Rewrap<WRITER>());
        }

        public void applyDefaults()
        {
        }

        public uint Id
        {
            get;
            set;
        }

        public ushort FromX
        {
            get;
            set;
        }

        public ushort FromY
        {
            get;
            set;
        }

        public ushort ToX
        {
            get;
            set;
        }

        public ushort ToY
        {
            get;
            set;
        }

        public CapnpGen.MoveType MoveType
        {
            get;
            set;
        }

        public bool PieceIsWhite
        {
            get;
            set;
        }

        public struct READER
        {
            readonly DeserializerState ctx;
            public READER(DeserializerState ctx)
            {
                this.ctx = ctx;
            }

            public static READER create(DeserializerState ctx) => new READER(ctx);
            public static implicit operator DeserializerState(READER reader) => reader.ctx;
            public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            public uint Id => ctx.ReadDataUInt(0UL, 0U);
            public ushort FromX => ctx.ReadDataUShort(32UL, (ushort)0);
            public ushort FromY => ctx.ReadDataUShort(48UL, (ushort)0);
            public ushort ToX => ctx.ReadDataUShort(64UL, (ushort)0);
            public ushort ToY => ctx.ReadDataUShort(80UL, (ushort)0);
            public CapnpGen.MoveType MoveType => (CapnpGen.MoveType)ctx.ReadDataUShort(96UL, (ushort)0);
            public bool PieceIsWhite => ctx.ReadDataBool(112UL, false);
        }

        public class WRITER : SerializerState
        {
            public WRITER()
            {
                this.SetStruct(2, 0);
            }

            public uint Id
            {
                get => this.ReadDataUInt(0UL, 0U);
                set => this.WriteData(0UL, value, 0U);
            }

            public ushort FromX
            {
                get => this.ReadDataUShort(32UL, (ushort)0);
                set => this.WriteData(32UL, value, (ushort)0);
            }

            public ushort FromY
            {
                get => this.ReadDataUShort(48UL, (ushort)0);
                set => this.WriteData(48UL, value, (ushort)0);
            }

            public ushort ToX
            {
                get => this.ReadDataUShort(64UL, (ushort)0);
                set => this.WriteData(64UL, value, (ushort)0);
            }

            public ushort ToY
            {
                get => this.ReadDataUShort(80UL, (ushort)0);
                set => this.WriteData(80UL, value, (ushort)0);
            }

            public CapnpGen.MoveType MoveType
            {
                get => (CapnpGen.MoveType)this.ReadDataUShort(96UL, (ushort)0);
                set => this.WriteData(96UL, (ushort)value, (ushort)0);
            }

            public bool PieceIsWhite
            {
                get => this.ReadDataBool(112UL, false);
                set => this.WriteData(112UL, value, false);
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xa26492155ea5b32eUL)]
    public class MoveResult : ICapnpSerializable
    {
        public const UInt64 typeId = 0xa26492155ea5b32eUL;
        void ICapnpSerializable.Deserialize(DeserializerState arg_)
        {
            var reader = READER.create(arg_);
            Seqnum = reader.Seqnum;
            CapturedPieceId = reader.CapturedPieceId;
            applyDefaults();
        }

        public void serialize(WRITER writer)
        {
            writer.Seqnum = Seqnum;
            writer.CapturedPieceId = CapturedPieceId;
        }

        void ICapnpSerializable.Serialize(SerializerState arg_)
        {
            serialize(arg_.Rewrap<WRITER>());
        }

        public void applyDefaults()
        {
        }

        public ulong Seqnum
        {
            get;
            set;
        }

        public uint CapturedPieceId
        {
            get;
            set;
        }

        public struct READER
        {
            readonly DeserializerState ctx;
            public READER(DeserializerState ctx)
            {
                this.ctx = ctx;
            }

            public static READER create(DeserializerState ctx) => new READER(ctx);
            public static implicit operator DeserializerState(READER reader) => reader.ctx;
            public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            public ulong Seqnum => ctx.ReadDataULong(0UL, 0UL);
            public uint CapturedPieceId => ctx.ReadDataUInt(64UL, 0U);
        }

        public class WRITER : SerializerState
        {
            public WRITER()
            {
                this.SetStruct(2, 0);
            }

            public ulong Seqnum
            {
                get => this.ReadDataULong(0UL, 0UL);
                set => this.WriteData(0UL, value, 0UL);
            }

            public uint CapturedPieceId
            {
                get => this.ReadDataUInt(64UL, 0U);
                set => this.WriteData(64UL, value, 0U);
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0x9e4c88ac96f28052UL)]
    public class RemoteMove : ICapnpSerializable
    {
        public const UInt64 typeId = 0x9e4c88ac96f28052UL;
        void ICapnpSerializable.Deserialize(DeserializerState arg_)
        {
            var reader = READER.create(arg_);
            Seqnum = reader.Seqnum;
            X = reader.X;
            Y = reader.Y;
            Data = CapnpSerializable.Create<CapnpGen.PieceData>(reader.Data);
            applyDefaults();
        }

        public void serialize(WRITER writer)
        {
            writer.Seqnum = Seqnum;
            writer.X = X;
            writer.Y = Y;
            Data?.serialize(writer.Data);
        }

        void ICapnpSerializable.Serialize(SerializerState arg_)
        {
            serialize(arg_.Rewrap<WRITER>());
        }

        public void applyDefaults()
        {
        }

        public ulong Seqnum
        {
            get;
            set;
        }

        public ushort X
        {
            get;
            set;
        }

        public ushort Y
        {
            get;
            set;
        }

        public CapnpGen.PieceData Data
        {
            get;
            set;
        }

        public struct READER
        {
            readonly DeserializerState ctx;
            public READER(DeserializerState ctx)
            {
                this.ctx = ctx;
            }

            public static READER create(DeserializerState ctx) => new READER(ctx);
            public static implicit operator DeserializerState(READER reader) => reader.ctx;
            public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            public ulong Seqnum => ctx.ReadDataULong(0UL, 0UL);
            public ushort X => ctx.ReadDataUShort(64UL, (ushort)0);
            public ushort Y => ctx.ReadDataUShort(80UL, (ushort)0);
            public CapnpGen.PieceData.READER Data => ctx.ReadStruct(0, CapnpGen.PieceData.READER.create);
        }

        public class WRITER : SerializerState
        {
            public WRITER()
            {
                this.SetStruct(2, 1);
            }

            public ulong Seqnum
            {
                get => this.ReadDataULong(0UL, 0UL);
                set => this.WriteData(0UL, value, 0UL);
            }

            public ushort X
            {
                get => this.ReadDataUShort(64UL, (ushort)0);
                set => this.WriteData(64UL, value, (ushort)0);
            }

            public ushort Y
            {
                get => this.ReadDataUShort(80UL, (ushort)0);
                set => this.WriteData(80UL, value, (ushort)0);
            }

            public CapnpGen.PieceData.WRITER Data
            {
                get => BuildPointer<CapnpGen.PieceData.WRITER>(0);
                set => Link(0, value);
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xcd7e2e8a7a7319d9UL), Proxy(typeof(Callback_Proxy)), Skeleton(typeof(Callback_Skeleton))]
    public interface ICallback : IDisposable
    {
        Task OnSnapshot(CapnpGen.Snapshot snapshot, CancellationToken cancellationToken_ = default);
        Task OnPiecesMoved(IReadOnlyList<CapnpGen.RemoteMove> moves, CancellationToken cancellationToken_ = default);
        Task OnPiecesCaptured(IReadOnlyList<CapnpGen.MoveResult> captures, CancellationToken cancellationToken_ = default);
        Task OnPiecesAdopted(IReadOnlyList<uint> ids, CancellationToken cancellationToken_ = default);
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xcd7e2e8a7a7319d9UL)]
    public class Callback_Proxy : Proxy, ICallback
    {
        public async Task OnSnapshot(CapnpGen.Snapshot snapshot, CancellationToken cancellationToken_ = default)
        {
            var in_ = SerializerState.CreateForRpc<CapnpGen.Callback.Params_OnSnapshot.WRITER>();
            var arg_ = new CapnpGen.Callback.Params_OnSnapshot()
            {Snapshot = snapshot};
            arg_?.serialize(in_);
            using (var d_ = await Call(14807323797135497689UL, 0, in_.Rewrap<DynamicSerializerState>(), false, cancellationToken_).WhenReturned)
            {
                var r_ = CapnpSerializable.Create<CapnpGen.Callback.Result_OnSnapshot>(d_);
                return;
            }
        }

        public async Task OnPiecesMoved(IReadOnlyList<CapnpGen.RemoteMove> moves, CancellationToken cancellationToken_ = default)
        {
            var in_ = SerializerState.CreateForRpc<CapnpGen.Callback.Params_OnPiecesMoved.WRITER>();
            var arg_ = new CapnpGen.Callback.Params_OnPiecesMoved()
            {Moves = moves};
            arg_?.serialize(in_);
            using (var d_ = await Call(14807323797135497689UL, 1, in_.Rewrap<DynamicSerializerState>(), false, cancellationToken_).WhenReturned)
            {
                var r_ = CapnpSerializable.Create<CapnpGen.Callback.Result_OnPiecesMoved>(d_);
                return;
            }
        }

        public async Task OnPiecesCaptured(IReadOnlyList<CapnpGen.MoveResult> captures, CancellationToken cancellationToken_ = default)
        {
            var in_ = SerializerState.CreateForRpc<CapnpGen.Callback.Params_OnPiecesCaptured.WRITER>();
            var arg_ = new CapnpGen.Callback.Params_OnPiecesCaptured()
            {Captures = captures};
            arg_?.serialize(in_);
            using (var d_ = await Call(14807323797135497689UL, 2, in_.Rewrap<DynamicSerializerState>(), false, cancellationToken_).WhenReturned)
            {
                var r_ = CapnpSerializable.Create<CapnpGen.Callback.Result_OnPiecesCaptured>(d_);
                return;
            }
        }

        public async Task OnPiecesAdopted(IReadOnlyList<uint> ids, CancellationToken cancellationToken_ = default)
        {
            var in_ = SerializerState.CreateForRpc<CapnpGen.Callback.Params_OnPiecesAdopted.WRITER>();
            var arg_ = new CapnpGen.Callback.Params_OnPiecesAdopted()
            {Ids = ids};
            arg_?.serialize(in_);
            using (var d_ = await Call(14807323797135497689UL, 3, in_.Rewrap<DynamicSerializerState>(), false, cancellationToken_).WhenReturned)
            {
                var r_ = CapnpSerializable.Create<CapnpGen.Callback.Result_OnPiecesAdopted>(d_);
                return;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xcd7e2e8a7a7319d9UL)]
    public class Callback_Skeleton : Skeleton<ICallback>
    {
        public Callback_Skeleton()
        {
            SetMethodTable(OnSnapshot, OnPiecesMoved, OnPiecesCaptured, OnPiecesAdopted);
        }

        public override ulong InterfaceId => 14807323797135497689UL;
        async Task<AnswerOrCounterquestion> OnSnapshot(DeserializerState d_, CancellationToken cancellationToken_)
        {
            using (d_)
            {
                var in_ = CapnpSerializable.Create<CapnpGen.Callback.Params_OnSnapshot>(d_);
                await Impl.OnSnapshot(in_.Snapshot, cancellationToken_);
                var s_ = SerializerState.CreateForRpc<CapnpGen.Callback.Result_OnSnapshot.WRITER>();
                return s_;
            }
        }

        async Task<AnswerOrCounterquestion> OnPiecesMoved(DeserializerState d_, CancellationToken cancellationToken_)
        {
            using (d_)
            {
                var in_ = CapnpSerializable.Create<CapnpGen.Callback.Params_OnPiecesMoved>(d_);
                await Impl.OnPiecesMoved(in_.Moves, cancellationToken_);
                var s_ = SerializerState.CreateForRpc<CapnpGen.Callback.Result_OnPiecesMoved.WRITER>();
                return s_;
            }
        }

        async Task<AnswerOrCounterquestion> OnPiecesCaptured(DeserializerState d_, CancellationToken cancellationToken_)
        {
            using (d_)
            {
                var in_ = CapnpSerializable.Create<CapnpGen.Callback.Params_OnPiecesCaptured>(d_);
                await Impl.OnPiecesCaptured(in_.Captures, cancellationToken_);
                var s_ = SerializerState.CreateForRpc<CapnpGen.Callback.Result_OnPiecesCaptured.WRITER>();
                return s_;
            }
        }

        async Task<AnswerOrCounterquestion> OnPiecesAdopted(DeserializerState d_, CancellationToken cancellationToken_)
        {
            using (d_)
            {
                var in_ = CapnpSerializable.Create<CapnpGen.Callback.Params_OnPiecesAdopted>(d_);
                await Impl.OnPiecesAdopted(in_.Ids, cancellationToken_);
                var s_ = SerializerState.CreateForRpc<CapnpGen.Callback.Result_OnPiecesAdopted.WRITER>();
                return s_;
            }
        }
    }

    public static class Callback
    {
        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xd24988be97d351c9UL)]
        public class Params_OnSnapshot : ICapnpSerializable
        {
            public const UInt64 typeId = 0xd24988be97d351c9UL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                Snapshot = CapnpSerializable.Create<CapnpGen.Snapshot>(reader.Snapshot);
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
                Snapshot?.serialize(writer.Snapshot);
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public CapnpGen.Snapshot Snapshot
            {
                get;
                set;
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
                public CapnpGen.Snapshot.READER Snapshot => ctx.ReadStruct(0, CapnpGen.Snapshot.READER.create);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 1);
                }

                public CapnpGen.Snapshot.WRITER Snapshot
                {
                    get => BuildPointer<CapnpGen.Snapshot.WRITER>(0);
                    set => Link(0, value);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0x9e2a7141096ea389UL)]
        public class Result_OnSnapshot : ICapnpSerializable
        {
            public const UInt64 typeId = 0x9e2a7141096ea389UL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 0);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xc6c03fe81f39a76aUL)]
        public class Params_OnPiecesMoved : ICapnpSerializable
        {
            public const UInt64 typeId = 0xc6c03fe81f39a76aUL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                Moves = reader.Moves?.ToReadOnlyList(_ => CapnpSerializable.Create<CapnpGen.RemoteMove>(_));
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
                writer.Moves.Init(Moves, (_s1, _v1) => _v1?.serialize(_s1));
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public IReadOnlyList<CapnpGen.RemoteMove> Moves
            {
                get;
                set;
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
                public IReadOnlyList<CapnpGen.RemoteMove.READER> Moves => ctx.ReadList(0).Cast(CapnpGen.RemoteMove.READER.create);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 1);
                }

                public ListOfStructsSerializer<CapnpGen.RemoteMove.WRITER> Moves
                {
                    get => BuildPointer<ListOfStructsSerializer<CapnpGen.RemoteMove.WRITER>>(0);
                    set => Link(0, value);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xdabac01b3777f48aUL)]
        public class Result_OnPiecesMoved : ICapnpSerializable
        {
            public const UInt64 typeId = 0xdabac01b3777f48aUL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 0);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0x886f202925e9e41bUL)]
        public class Params_OnPiecesCaptured : ICapnpSerializable
        {
            public const UInt64 typeId = 0x886f202925e9e41bUL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                Captures = reader.Captures?.ToReadOnlyList(_ => CapnpSerializable.Create<CapnpGen.MoveResult>(_));
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
                writer.Captures.Init(Captures, (_s1, _v1) => _v1?.serialize(_s1));
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public IReadOnlyList<CapnpGen.MoveResult> Captures
            {
                get;
                set;
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
                public IReadOnlyList<CapnpGen.MoveResult.READER> Captures => ctx.ReadList(0).Cast(CapnpGen.MoveResult.READER.create);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 1);
                }

                public ListOfStructsSerializer<CapnpGen.MoveResult.WRITER> Captures
                {
                    get => BuildPointer<ListOfStructsSerializer<CapnpGen.MoveResult.WRITER>>(0);
                    set => Link(0, value);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xffb7c24e412e328cUL)]
        public class Result_OnPiecesCaptured : ICapnpSerializable
        {
            public const UInt64 typeId = 0xffb7c24e412e328cUL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 0);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xb9549c47fa672c11UL)]
        public class Params_OnPiecesAdopted : ICapnpSerializable
        {
            public const UInt64 typeId = 0xb9549c47fa672c11UL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                Ids = reader.Ids;
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
                writer.Ids.Init(Ids);
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public IReadOnlyList<uint> Ids
            {
                get;
                set;
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
                public IReadOnlyList<uint> Ids => ctx.ReadList(0).CastUInt();
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 1);
                }

                public ListOfPrimitivesSerializer<uint> Ids
                {
                    get => BuildPointer<ListOfPrimitivesSerializer<uint>>(0);
                    set => Link(0, value);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xc06dcb993fcbeaf3UL)]
        public class Result_OnPiecesAdopted : ICapnpSerializable
        {
            public const UInt64 typeId = 0xc06dcb993fcbeaf3UL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 0);
                }
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xb2613225c67ed861UL), Proxy(typeof(Firehorse_Proxy)), Skeleton(typeof(Firehorse_Skeleton))]
    public interface IFirehorse : IDisposable
    {
        Task Listen(CapnpGen.ICallback callback, CancellationToken cancellationToken_ = default);
        Task<(bool, IReadOnlyList<CapnpGen.MoveResult>, ushort)> MoveSequential(IReadOnlyList<CapnpGen.Move> moves, CancellationToken cancellationToken_ = default);
        Task<(bool, IReadOnlyList<CapnpGen.MoveResult>, IReadOnlyList<ushort>)> MoveParallel(IReadOnlyList<CapnpGen.Move> moves, CancellationToken cancellationToken_ = default);
        Task Queue(ushort x, ushort y, CancellationToken cancellationToken_ = default);
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xb2613225c67ed861UL)]
    public class Firehorse_Proxy : Proxy, IFirehorse
    {
        public async Task Listen(CapnpGen.ICallback callback, CancellationToken cancellationToken_ = default)
        {
            var in_ = SerializerState.CreateForRpc<CapnpGen.Firehorse.Params_Listen.WRITER>();
            var arg_ = new CapnpGen.Firehorse.Params_Listen()
            {Callback = callback};
            arg_?.serialize(in_);
            using (var d_ = await Call(12853609949317486689UL, 0, in_.Rewrap<DynamicSerializerState>(), false, cancellationToken_).WhenReturned)
            {
                var r_ = CapnpSerializable.Create<CapnpGen.Firehorse.Result_Listen>(d_);
                return;
            }
        }

        public async Task<(bool, IReadOnlyList<CapnpGen.MoveResult>, ushort)> MoveSequential(IReadOnlyList<CapnpGen.Move> moves, CancellationToken cancellationToken_ = default)
        {
            var in_ = SerializerState.CreateForRpc<CapnpGen.Firehorse.Params_MoveSequential.WRITER>();
            var arg_ = new CapnpGen.Firehorse.Params_MoveSequential()
            {Moves = moves};
            arg_?.serialize(in_);
            using (var d_ = await Call(12853609949317486689UL, 1, in_.Rewrap<DynamicSerializerState>(), false, cancellationToken_).WhenReturned)
            {
                var r_ = CapnpSerializable.Create<CapnpGen.Firehorse.Result_MoveSequential>(d_);
                return (r_.Success, r_.Results, r_.FailedAt);
            }
        }

        public async Task<(bool, IReadOnlyList<CapnpGen.MoveResult>, IReadOnlyList<ushort>)> MoveParallel(IReadOnlyList<CapnpGen.Move> moves, CancellationToken cancellationToken_ = default)
        {
            var in_ = SerializerState.CreateForRpc<CapnpGen.Firehorse.Params_MoveParallel.WRITER>();
            var arg_ = new CapnpGen.Firehorse.Params_MoveParallel()
            {Moves = moves};
            arg_?.serialize(in_);
            using (var d_ = await Call(12853609949317486689UL, 2, in_.Rewrap<DynamicSerializerState>(), false, cancellationToken_).WhenReturned)
            {
                var r_ = CapnpSerializable.Create<CapnpGen.Firehorse.Result_MoveParallel>(d_);
                return (r_.Success, r_.Results, r_.Failed);
            }
        }

        public async Task Queue(ushort x, ushort y, CancellationToken cancellationToken_ = default)
        {
            var in_ = SerializerState.CreateForRpc<CapnpGen.Firehorse.Params_Queue.WRITER>();
            var arg_ = new CapnpGen.Firehorse.Params_Queue()
            {X = x, Y = y};
            arg_?.serialize(in_);
            using (var d_ = await Call(12853609949317486689UL, 3, in_.Rewrap<DynamicSerializerState>(), false, cancellationToken_).WhenReturned)
            {
                var r_ = CapnpSerializable.Create<CapnpGen.Firehorse.Result_Queue>(d_);
                return;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xb2613225c67ed861UL)]
    public class Firehorse_Skeleton : Skeleton<IFirehorse>
    {
        public Firehorse_Skeleton()
        {
            SetMethodTable(Listen, MoveSequential, MoveParallel, Queue);
        }

        public override ulong InterfaceId => 12853609949317486689UL;
        async Task<AnswerOrCounterquestion> Listen(DeserializerState d_, CancellationToken cancellationToken_)
        {
            using (d_)
            {
                var in_ = CapnpSerializable.Create<CapnpGen.Firehorse.Params_Listen>(d_);
                await Impl.Listen(in_.Callback, cancellationToken_);
                var s_ = SerializerState.CreateForRpc<CapnpGen.Firehorse.Result_Listen.WRITER>();
                return s_;
            }
        }

        Task<AnswerOrCounterquestion> MoveSequential(DeserializerState d_, CancellationToken cancellationToken_)
        {
            using (d_)
            {
                var in_ = CapnpSerializable.Create<CapnpGen.Firehorse.Params_MoveSequential>(d_);
                return Impatient.MaybeTailCall(Impl.MoveSequential(in_.Moves, cancellationToken_), (success, results, failedAt) =>
                {
                    var s_ = SerializerState.CreateForRpc<CapnpGen.Firehorse.Result_MoveSequential.WRITER>();
                    var r_ = new CapnpGen.Firehorse.Result_MoveSequential{Success = success, Results = results, FailedAt = failedAt};
                    r_.serialize(s_);
                    return s_;
                }

                );
            }
        }

        Task<AnswerOrCounterquestion> MoveParallel(DeserializerState d_, CancellationToken cancellationToken_)
        {
            using (d_)
            {
                var in_ = CapnpSerializable.Create<CapnpGen.Firehorse.Params_MoveParallel>(d_);
                return Impatient.MaybeTailCall(Impl.MoveParallel(in_.Moves, cancellationToken_), (success, results, failed) =>
                {
                    var s_ = SerializerState.CreateForRpc<CapnpGen.Firehorse.Result_MoveParallel.WRITER>();
                    var r_ = new CapnpGen.Firehorse.Result_MoveParallel{Success = success, Results = results, Failed = failed};
                    r_.serialize(s_);
                    return s_;
                }

                );
            }
        }

        async Task<AnswerOrCounterquestion> Queue(DeserializerState d_, CancellationToken cancellationToken_)
        {
            using (d_)
            {
                var in_ = CapnpSerializable.Create<CapnpGen.Firehorse.Params_Queue>(d_);
                await Impl.Queue(in_.X, in_.Y, cancellationToken_);
                var s_ = SerializerState.CreateForRpc<CapnpGen.Firehorse.Result_Queue.WRITER>();
                return s_;
            }
        }
    }

    public static class Firehorse
    {
        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xa6585b7d2f4bac2eUL)]
        public class Params_Listen : ICapnpSerializable
        {
            public const UInt64 typeId = 0xa6585b7d2f4bac2eUL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                Callback = reader.Callback;
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
                writer.Callback = Callback;
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public CapnpGen.ICallback Callback
            {
                get;
                set;
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
                public CapnpGen.ICallback Callback => ctx.ReadCap<CapnpGen.ICallback>(0);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 1);
                }

                public CapnpGen.ICallback Callback
                {
                    get => ReadCap<CapnpGen.ICallback>(0);
                    set => LinkObject(0, value);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0x993fed95f9b59463UL)]
        public class Result_Listen : ICapnpSerializable
        {
            public const UInt64 typeId = 0x993fed95f9b59463UL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 0);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0x8bfafaaffd9abba4UL)]
        public class Params_MoveSequential : ICapnpSerializable
        {
            public const UInt64 typeId = 0x8bfafaaffd9abba4UL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                Moves = reader.Moves?.ToReadOnlyList(_ => CapnpSerializable.Create<CapnpGen.Move>(_));
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
                writer.Moves.Init(Moves, (_s1, _v1) => _v1?.serialize(_s1));
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public IReadOnlyList<CapnpGen.Move> Moves
            {
                get;
                set;
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
                public IReadOnlyList<CapnpGen.Move.READER> Moves => ctx.ReadList(0).Cast(CapnpGen.Move.READER.create);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 1);
                }

                public ListOfStructsSerializer<CapnpGen.Move.WRITER> Moves
                {
                    get => BuildPointer<ListOfStructsSerializer<CapnpGen.Move.WRITER>>(0);
                    set => Link(0, value);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xc26fa367c2611c18UL)]
        public class Result_MoveSequential : ICapnpSerializable
        {
            public const UInt64 typeId = 0xc26fa367c2611c18UL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                Success = reader.Success;
                Results = reader.Results?.ToReadOnlyList(_ => CapnpSerializable.Create<CapnpGen.MoveResult>(_));
                FailedAt = reader.FailedAt;
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
                writer.Success = Success;
                writer.Results.Init(Results, (_s1, _v1) => _v1?.serialize(_s1));
                writer.FailedAt = FailedAt;
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public bool Success
            {
                get;
                set;
            }

            public IReadOnlyList<CapnpGen.MoveResult> Results
            {
                get;
                set;
            }

            public ushort FailedAt
            {
                get;
                set;
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
                public bool Success => ctx.ReadDataBool(0UL, false);
                public IReadOnlyList<CapnpGen.MoveResult.READER> Results => ctx.ReadList(0).Cast(CapnpGen.MoveResult.READER.create);
                public ushort FailedAt => ctx.ReadDataUShort(16UL, (ushort)0);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(1, 1);
                }

                public bool Success
                {
                    get => this.ReadDataBool(0UL, false);
                    set => this.WriteData(0UL, value, false);
                }

                public ListOfStructsSerializer<CapnpGen.MoveResult.WRITER> Results
                {
                    get => BuildPointer<ListOfStructsSerializer<CapnpGen.MoveResult.WRITER>>(0);
                    set => Link(0, value);
                }

                public ushort FailedAt
                {
                    get => this.ReadDataUShort(16UL, (ushort)0);
                    set => this.WriteData(16UL, value, (ushort)0);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xf1b7cc670c593f25UL)]
        public class Params_MoveParallel : ICapnpSerializable
        {
            public const UInt64 typeId = 0xf1b7cc670c593f25UL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                Moves = reader.Moves?.ToReadOnlyList(_ => CapnpSerializable.Create<CapnpGen.Move>(_));
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
                writer.Moves.Init(Moves, (_s1, _v1) => _v1?.serialize(_s1));
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public IReadOnlyList<CapnpGen.Move> Moves
            {
                get;
                set;
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
                public IReadOnlyList<CapnpGen.Move.READER> Moves => ctx.ReadList(0).Cast(CapnpGen.Move.READER.create);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 1);
                }

                public ListOfStructsSerializer<CapnpGen.Move.WRITER> Moves
                {
                    get => BuildPointer<ListOfStructsSerializer<CapnpGen.Move.WRITER>>(0);
                    set => Link(0, value);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0x87bf0a8e5c082ca1UL)]
        public class Result_MoveParallel : ICapnpSerializable
        {
            public const UInt64 typeId = 0x87bf0a8e5c082ca1UL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                Success = reader.Success;
                Results = reader.Results?.ToReadOnlyList(_ => CapnpSerializable.Create<CapnpGen.MoveResult>(_));
                Failed = reader.Failed;
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
                writer.Success = Success;
                writer.Results.Init(Results, (_s1, _v1) => _v1?.serialize(_s1));
                writer.Failed.Init(Failed);
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public bool Success
            {
                get;
                set;
            }

            public IReadOnlyList<CapnpGen.MoveResult> Results
            {
                get;
                set;
            }

            public IReadOnlyList<ushort> Failed
            {
                get;
                set;
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
                public bool Success => ctx.ReadDataBool(0UL, false);
                public IReadOnlyList<CapnpGen.MoveResult.READER> Results => ctx.ReadList(0).Cast(CapnpGen.MoveResult.READER.create);
                public IReadOnlyList<ushort> Failed => ctx.ReadList(1).CastUShort();
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(1, 2);
                }

                public bool Success
                {
                    get => this.ReadDataBool(0UL, false);
                    set => this.WriteData(0UL, value, false);
                }

                public ListOfStructsSerializer<CapnpGen.MoveResult.WRITER> Results
                {
                    get => BuildPointer<ListOfStructsSerializer<CapnpGen.MoveResult.WRITER>>(0);
                    set => Link(0, value);
                }

                public ListOfPrimitivesSerializer<ushort> Failed
                {
                    get => BuildPointer<ListOfPrimitivesSerializer<ushort>>(1);
                    set => Link(1, value);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xf810477b3398d373UL)]
        public class Params_Queue : ICapnpSerializable
        {
            public const UInt64 typeId = 0xf810477b3398d373UL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                X = reader.X;
                Y = reader.Y;
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
                writer.X = X;
                writer.Y = Y;
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public ushort X
            {
                get;
                set;
            }

            public ushort Y
            {
                get;
                set;
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
                public ushort X => ctx.ReadDataUShort(0UL, (ushort)0);
                public ushort Y => ctx.ReadDataUShort(16UL, (ushort)0);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(1, 0);
                }

                public ushort X
                {
                    get => this.ReadDataUShort(0UL, (ushort)0);
                    set => this.WriteData(0UL, value, (ushort)0);
                }

                public ushort Y
                {
                    get => this.ReadDataUShort(16UL, (ushort)0);
                    set => this.WriteData(16UL, value, (ushort)0);
                }
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xa6edbf6f958b9852UL)]
        public class Result_Queue : ICapnpSerializable
        {
            public const UInt64 typeId = 0xa6edbf6f958b9852UL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                applyDefaults();
            }

            public void serialize(WRITER writer)
            {
            }

            void ICapnpSerializable.Serialize(SerializerState arg_)
            {
                serialize(arg_.Rewrap<WRITER>());
            }

            public void applyDefaults()
            {
            }

            public struct READER
            {
                readonly DeserializerState ctx;
                public READER(DeserializerState ctx)
                {
                    this.ctx = ctx;
                }

                public static READER create(DeserializerState ctx) => new READER(ctx);
                public static implicit operator DeserializerState(READER reader) => reader.ctx;
                public static implicit operator READER(DeserializerState ctx) => new READER(ctx);
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
                    this.SetStruct(0, 0);
                }
            }
        }
    }
}