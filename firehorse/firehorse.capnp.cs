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

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0x8625d1be53206cfeUL)]
    public class Piece : ICapnpSerializable
    {
        public const UInt64 typeId = 0x8625d1be53206cfeUL;
        void ICapnpSerializable.Deserialize(DeserializerState arg_)
        {
            var reader = READER.create(arg_);
            Id = reader.Id;
            Dx = reader.Dx;
            Dy = reader.Dy;
            Type = reader.Type;
            IsWhite = reader.IsWhite;
            applyDefaults();
        }

        public void serialize(WRITER writer)
        {
            writer.Id = Id;
            writer.Dx = Dx;
            writer.Dy = Dy;
            writer.Type = Type;
            writer.IsWhite = IsWhite;
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
            public sbyte Dx => ctx.ReadDataSByte(32UL, (sbyte)0);
            public sbyte Dy => ctx.ReadDataSByte(40UL, (sbyte)0);
            public CapnpGen.PieceType Type => (CapnpGen.PieceType)ctx.ReadDataUShort(48UL, (ushort)0);
            public bool IsWhite => ctx.ReadDataBool(64UL, false);
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

            public sbyte Dx
            {
                get => this.ReadDataSByte(32UL, (sbyte)0);
                set => this.WriteData(32UL, value, (sbyte)0);
            }

            public sbyte Dy
            {
                get => this.ReadDataSByte(40UL, (sbyte)0);
                set => this.WriteData(40UL, value, (sbyte)0);
            }

            public CapnpGen.PieceType Type
            {
                get => (CapnpGen.PieceType)this.ReadDataUShort(48UL, (ushort)0);
                set => this.WriteData(48UL, (ushort)value, (ushort)0);
            }

            public bool IsWhite
            {
                get => this.ReadDataBool(64UL, false);
                set => this.WriteData(64UL, value, false);
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
            Pieces = reader.Pieces?.ToReadOnlyList(_ => CapnpSerializable.Create<CapnpGen.Piece>(_));
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

        public IReadOnlyList<CapnpGen.Piece> Pieces
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
            public IReadOnlyList<CapnpGen.Piece.READER> Pieces => ctx.ReadList(0).Cast(CapnpGen.Piece.READER.create);
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

            public ListOfStructsSerializer<CapnpGen.Piece.WRITER> Pieces
            {
                get => BuildPointer<ListOfStructsSerializer<CapnpGen.Piece.WRITER>>(0);
                set => Link(0, value);
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0x842a51f37a799415UL)]
    public class Command : ICapnpSerializable
    {
        public const UInt64 typeId = 0x842a51f37a799415UL;
        public enum WHICH : ushort
        {
            Move = 0,
            Watch = 1,
            undefined = 65535
        }

        void ICapnpSerializable.Deserialize(DeserializerState arg_)
        {
            var reader = READER.create(arg_);
            switch (reader.which)
            {
                case WHICH.Move:
                    Move = CapnpSerializable.Create<CapnpGen.Command.move>(reader.Move);
                    break;
                case WHICH.Watch:
                    Watch = CapnpSerializable.Create<CapnpGen.Command.watch>(reader.Watch);
                    break;
            }

            applyDefaults();
        }

        private WHICH _which = WHICH.undefined;
        private object _content;
        public WHICH which
        {
            get => _which;
            set
            {
                if (value == _which)
                    return;
                _which = value;
                switch (value)
                {
                    case WHICH.Move:
                        _content = null;
                        break;
                    case WHICH.Watch:
                        _content = null;
                        break;
                }
            }
        }

        public void serialize(WRITER writer)
        {
            writer.which = which;
            switch (which)
            {
                case WHICH.Move:
                    Move?.serialize(writer.Move);
                    break;
                case WHICH.Watch:
                    Watch?.serialize(writer.Watch);
                    break;
            }
        }

        void ICapnpSerializable.Serialize(SerializerState arg_)
        {
            serialize(arg_.Rewrap<WRITER>());
        }

        public void applyDefaults()
        {
        }

        public CapnpGen.Command.move Move
        {
            get => _which == WHICH.Move ? (CapnpGen.Command.move)_content : null;
            set
            {
                _which = WHICH.Move;
                _content = value;
            }
        }

        public CapnpGen.Command.watch Watch
        {
            get => _which == WHICH.Watch ? (CapnpGen.Command.watch)_content : null;
            set
            {
                _which = WHICH.Watch;
                _content = value;
            }
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
            public WHICH which => (WHICH)ctx.ReadDataUShort(128U, (ushort)0);
            public move.READER Move => which == WHICH.Move ? new move.READER(ctx) : default;
            public watch.READER Watch => which == WHICH.Watch ? new watch.READER(ctx) : default;
        }

        public class WRITER : SerializerState
        {
            public WRITER()
            {
                this.SetStruct(3, 0);
            }

            public WHICH which
            {
                get => (WHICH)this.ReadDataUShort(128U, (ushort)0);
                set => this.WriteData(128U, (ushort)value, (ushort)0);
            }

            public move.WRITER Move
            {
                get => which == WHICH.Move ? Rewrap<move.WRITER>() : default;
            }

            public watch.WRITER Watch
            {
                get => which == WHICH.Watch ? Rewrap<watch.WRITER>() : default;
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0x832504db7106750bUL)]
        public class move : ICapnpSerializable
        {
            public const UInt64 typeId = 0x832504db7106750bUL;
            void ICapnpSerializable.Deserialize(DeserializerState arg_)
            {
                var reader = READER.create(arg_);
                Id = reader.Id;
                FromX = reader.FromX;
                FromY = reader.FromY;
                ToX = reader.ToX;
                ToY = reader.ToY;
                MoveType = reader.MoveType;
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
            }

            public class WRITER : SerializerState
            {
                public WRITER()
                {
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
            }
        }

        [System.CodeDom.Compiler.GeneratedCode("capnpc-csharp", "1.3.0.0"), TypeId(0xbb0bcfa516851b7bUL)]
        public class watch : ICapnpSerializable
        {
            public const UInt64 typeId = 0xbb0bcfa516851b7bUL;
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
    }
}