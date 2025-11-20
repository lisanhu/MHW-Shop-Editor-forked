# Monster Hunter: World shopList (.slt) Binary Layout

This document captures everything that is currently known about the `shopList.slt`
format used by Monster Hunter: World’s “Provisions Stockpile” shop. It is based on
reverse engineering of the untouched game files (`shopList_00.slt … shopList_10.slt`)
shipped with the project in `binaries/`.

Keeping this reference next to the codebase makes it easier to reason about the
encoder/decoder and to verify future changes.

---

## File structure (high level)

```
+----------------------+ 0x0000
| File Header (10 B)   |
+----------------------+ 0x000A
| Entry 0 (14 B)       |
+----------------------+ 0x0018
| Entry 1 (14 B)       |
+----------------------+
| ... repeats ...      |
+----------------------+
```

* All integers are little-endian.
* The file consists of a 10-byte header followed by 14-byte rows.
* Each row corresponds to a single item slot.
* The file size is `10 + N * 14`, where N is the number of items.

---

## Header

| Offset | Size | Notes                                                                                |
|--------|------|--------------------------------------------------------------------------------------|
| 0x00   | 4    | Magic/version combo. In all retail files this is `0x01100918`. Leave untouched.      |
| 0x04   | 2    | Build/region marker (commonly `0x0019`).                                             |
| 0x06   | 2    | Flags (always `0x00FF` in stock data).                                               |
| 0x08   | 2    | Reserved.                                                                            |

**Round‑trip rule:** When loading an existing file, keep bytes `0x00–0x09` exactly as read.

---

## Entry layout (14 bytes per row)

Each 14-byte row corresponds to one item and contains 7 `uint16` words:

```
Word 0 (+0x00) : Row Index (used for chaining)
Word 1 (+0x02) : Reserved (usually 0)
Word 2 (+0x04) : Current Item ID
Word 3 (+0x06) : Reserved (usually 0)
Word 4 (+0x08) : Reserved (usually 0)
Word 5 (+0x0A) : Reserved (usually 0)
Word 6 (+0x0C) : Row Index (duplicate of Word 0)
```

### Practical guidance

* The primary item ID is located at **offset +4** (Word 2).
* The value at **offset +0** (Word 0) and **offset +12** (Word 6) is the index of the row (e.g. 0 for 1st row, 1 for 2nd row, etc.).
* When writing:
  * Preserve the row template (Words 1, 3, 4, 5).
  * Write the new Item ID to Word 2.
  * Write the row index to Word 0 and Word 6 to maintain the chain structure.

---

## Counting items

* Each 14-byte row is one item.
* `shopList_01.slt` is 3580 bytes -> (3580 - 10) / 14 = 255 items.
* `shopList_10.slt` is 3146 bytes -> (3146 - 10) / 14 = 224 items.

---

## Example (first row of `shopList_01.slt`)

Row 0 (offset 0x0A):
```
00 00  (Index: 0)
00 00  (Res)
00 01  (Item ID: 256)
00 00  (Res)
00 00  (Res)
00 00  (Res)
00 00  (Index: 0)
```

Row 1 (offset 0x18):
```
01 00  (Index: 1)
00 00  (Res)
01 01  (Item ID: 257)
00 00  (Res)
00 00  (Res)
00 00  (Res)
01 00  (Index: 1)
```

## Recommendations for the codebase

1. **Header:** 10 bytes.
2. **Rows:** 14 bytes.
3. **Logic:** `Item ID` at offset +4. `Index` at offset +0 and +12.

With this knowledge encoded in both documentation and code, we can finally load and
save `.slt` files without breaking the game’s expectations.