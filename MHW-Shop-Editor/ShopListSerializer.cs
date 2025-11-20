using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace MHWShopEditor
{
    /// <summary>
    /// Binary helper for Monster Hunter: World shopList (*.slt) files.
    /// Layout:
    ///   - 10-byte header copied verbatim
    ///   - Repeated 14-byte rows. Each row is one item.
    ///   - Row layout (7 ushorts): Index, 0, ItemID, 0, 0, 0, Index
    /// </summary>
    public static class ShopListSerializer
    {
        private const int HeaderLength = 10;
        private const int RowLength = 14;
        private const int MaxSlots = 256;

        public static ShopListDocument Read(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length < HeaderLength)
                throw new ArgumentException("File too small to be a valid shop list.", nameof(data));

            int payloadLength = data.Length - HeaderLength;
            if (payloadLength % RowLength != 0)
                throw new ArgumentException("Unexpected payload size; rows must be 14-byte aligned.", nameof(data));

            var header = new byte[HeaderLength];
            Buffer.BlockCopy(data, 0, header, 0, HeaderLength);

            var rows = new List<byte[]>();
            var items = new List<ushort>(MaxSlots);

            var span = data.AsSpan(HeaderLength);
            for (int offset = 0; offset < span.Length; offset += RowLength)
            {
                var row = span.Slice(offset, RowLength).ToArray();
                rows.Add(row);

                // Item ID is at offset 4 (Word 2)
                ushort itemId = BinaryPrimitives.ReadUInt16LittleEndian(row.AsSpan(4, 2));
                items.Add(itemId);
            }

            return new ShopListDocument(header, rows, items);
        }

        public static byte[] Write(ShopListDocument document, IReadOnlyList<uint> slotIds)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (slotIds == null) throw new ArgumentNullException(nameof(slotIds));
            if (slotIds.Count > MaxSlots)
                throw new ArgumentException($"Shop cannot exceed {MaxSlots} items.", nameof(slotIds));

            var rows = CloneRows(document.Rows);

            // Ensure we have enough template rows for the new items
            while (rows.Count < slotIds.Count)
            {
                rows.Add(new byte[RowLength]);
            }

            // We only write as many rows as there are items
            int outputRowCount = slotIds.Count;
            var result = new byte[HeaderLength + outputRowCount * RowLength];

            Buffer.BlockCopy(document.Header, 0, result, 0, HeaderLength);

            for (int i = 0; i < outputRowCount; i++)
            {
                var destination = result.AsSpan(HeaderLength + i * RowLength, RowLength);

                // Copy template (preserves 0s and other potential metadata if present)
                rows[i].CopyTo(destination);

                ushort id = (ushort)(slotIds[i] & 0xFFFF);
                ushort index = (ushort)i;

                // Write Index at +0 and +12
                BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0, 2), index);
                BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(12, 2), index);

                // Write Item ID at +4
                BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(4, 2), id);
            }

            return result;
        }

        public static ShopListDocument CreateDefaultDocument()
        {
            // Default header based on observations (01 10 09 18 19 00 FF 00 00 00)
            byte[] header = { 0x01, 0x10, 0x09, 0x18, 0x19, 0x00, 0xFF, 0x00, 0x00, 0x00 };
            return new ShopListDocument(header, new List<byte[]>(), new List<ushort>());
        }

        private static List<byte[]> CloneRows(IReadOnlyList<byte[]> source)
        {
            var clones = new List<byte[]>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                clones.Add((byte[])source[i].Clone());
            }
            return clones;
        }
    }

    public sealed class ShopListDocument
    {
        internal ShopListDocument(byte[] header, List<byte[]> rows, List<ushort> itemIds)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
            ItemIds = itemIds ?? throw new ArgumentNullException(nameof(itemIds));
        }

        public byte[] Header { get; }

        public IReadOnlyList<byte[]> Rows { get; }

        public IReadOnlyList<ushort> ItemIds { get; }
    }
}
