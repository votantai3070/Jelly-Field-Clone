# Jelly Field

Jelly Field là game puzzle dạng kéo-thả trên lưới, nơi mỗi ô board chứa tối đa một `JellyPiece`, và mỗi `JellyPiece` gồm từ 1 đến 4 `JellySubCell`. Khi các `subcell` cùng màu của hai jelly ở hai ô kề nhau chạm cạnh thật, chúng sẽ được thu thập; sau khi bị xóa, jelly còn sống sẽ co/phình lại theo layout mới và tiếp tục được kiểm tra chain cho đến khi trạng thái board ổn định [1][2].

## Gameplay cốt lõi

- Board là lưới `width x height`; mỗi ô có thể rỗng hoặc chứa đúng một `JellyPiece`.
- Người chơi kéo jelly preview vào một ô trống trên board.
- Mỗi jelly có 1 đến 4 subcell, mỗi subcell có màu và `id` riêng.
- Merge không diễn ra theo cả khối jelly, mà diễn ra ở **cấp subcell**.
- Hai subcell được thu thập khi thỏa cả hai điều kiện: cùng màu, và hai hình chữ nhật runtime (`localRect`) của chúng chạm mép đúng hướng với overlap đủ lớn.
- Sau khi một subcell bị xóa, jelly còn lại được render lại ngay. Layout mới này có thể tạo ra lần chạm mới với hàng xóm, nên một lượt đặt có thể sinh ra nhiều đợt collect liên tiếp [1].

## Cấu trúc hệ thống

| Thành phần | Vai trò |
|---|---|
| `BoardManager` | Quản lý lưới, chuyển đổi grid/world, đặt và gỡ jelly khỏi ô. |
| `InputHandler` | Quản lý chạm, kéo-thả jelly preview, thả vào board. |
| `GameManager` | Điều phối lượt chơi, sinh jelly mới, resolve chain, kiểm tra thắng/thua. |
| `MergeSystem` | Kiểm tra tiếp xúc subcell giữa các jelly kề nhau. |
| `GoalSystem` | Theo dõi số subcell đã thu thập theo màu và điều kiện thắng. |
| `JellyPiece` | Dữ liệu và hành vi của một jelly đang nằm trên board hoặc ở preview. |
| `JellyPieceView` | Dựng hình các subcell con theo layout runtime. |
| `JellySubCell` | Đơn vị nhỏ nhất có thể merge/collect; có màu, `id`, `slot`, `localRect`. |

## Rule dữ liệu

### 1. Board và Cell

`BoardManager` tạo mảng `CellData[,] grid`, trong đó mỗi `CellData` lưu `Coord` và `OccupiedPiece`. Đây là trạng thái nguồn sự thật của board: một jelly chỉ được xem là đang nằm trên board khi một `CellData` trỏ tới nó [1].

### 2. JellyPiece

`JellyPiece` là container của danh sách `subCells`. Piece không tự biết merge; nó chỉ biết:

- đang ở ô nào (`CurrentCoord`),
- có đang thuộc board hay không (`HasCell`),
- danh sách subcell hiện tại,
- cách xóa một subcell theo `id`, rồi render lại hình dạng mới.

### 3. JellySubCell

Mỗi `JellySubCell` có:

- `id`: định danh duy nhất để xóa đúng subcell.
- `color`: màu logic dùng cho goal và merge.
- `slot`: nhãn layout runtime, chủ yếu hữu ích cho debug/animation.
- `localRect`: hình chữ nhật runtime trong hệ tọa độ local của jelly; đây là dữ liệu quan trọng nhất cho merge hình học.

### 4. Layout nội bộ của jelly

`JellyPieceView.BuildLayout(count)` quyết định cách 1, 2, 3 hoặc 4 subcell được sắp trong một ô chuẩn `[-0.5 .. 0.5]`. Khi render, từng subcell được gán `slot` và `localRect`, rồi sprite con được đặt đúng vị trí và scale tương ứng [1].

## Flow của một lượt chơi

### 1. Khởi tạo level

`GameManager.Start()` gọi:

1. `board.ConfigureBoard(levelData.width, levelData.height)` để tạo board đúng kích thước level.
2. `goalSystem.Initialize(levelData)` để nạp mục tiêu theo màu.
3. `inputHandler.SpawnNextPiece()` để sinh jelly preview đầu tiên.

### 2. Sinh jelly preview

`InputHandler.SpawnNextPiece()` instantiate `jellyPrefab`, rồi `GameManager.SetupSpawnedPiece()` tạo danh sách subcell ngẫu nhiên.

Rule random hiện tại:

- số subcell từ 1 đến 4,
- màu chọn từ `Red`, `Yellow`, `Blue`, `Green`,
- không cho phép hai subcell kề nhau **trong cùng một jelly** có cùng màu.

Điều này được xử lý bởi `GetRandomColorExceptAdjacent()` kết hợp với `AreSubCellsAdjacentInSameJelly(...)`.

### 3. Drag và đặt jelly vào board

`InputHandler` chỉ cho phép người chơi kéo đúng `currentPiece` preview. Khi thả tay:

1. `board.WorldToGrid(worldPos)` đổi tọa độ world sang ô lưới.
2. `board.TryPlacePiece(selectedPiece, coord)` kiểm tra ô có hợp lệ và đang trống hay không.
3. Nếu đặt thành công, piece được gắn vào `CellData`, cập nhật `CurrentCoord`, đưa transform về tâm ô, và phát animation landing.
4. `gameManager.ResolveTurn(placedPiece, coord)` bắt đầu lượt resolve [1].

## Flow check subcell và merge

### 1. Chỉ xét jelly ở 4 ô kề

`MergeSystem.TryGetTouchMatchesForPlacedPiece(placedCoord, out matches)` lấy jelly ở ô đang xét, rồi chỉ duyệt 4 hướng:

- Up
- Right
- Down
- Left

Không xét chéo ô, không xét subcell trong cùng một jelly.

### 2. So từng cặp subcell cùng màu

Với mỗi jelly hàng xóm, hệ thống duyệt toàn bộ cặp:

- `sourcePiece.SubCells[i]`
- `targetPiece.SubCells[j]`

Nếu một subcell không có layout runtime hợp lệ (`HasValidRuntimeLayout == false`) thì bỏ qua. Nếu màu khác nhau thì bỏ qua.

### 3. Kiểm tra hình học thật bằng Rect

Thay vì đoán bằng enum slot, merge hiện tại dùng `localRect` thật:

- Nếu hàng xóm ở trên: kiểm tra `a.yMax` gần `b.yMin` và overlap theo trục X đủ lớn.
- Nếu hàng xóm bên phải: kiểm tra `a.xMax` gần `b.xMin` và overlap theo trục Y đủ lớn.
- Tương tự cho dưới và trái.

Hai ngưỡng điều chỉnh:

- `edgeTolerance`: cho phép sai số nhỏ khi hai mép gần như trùng nhau.
- `overlapTolerance`: yêu cầu overlap tối thiểu để tránh merge giả ở góc.

### 4. Thu thập unique subcell

Nếu hai subcell hợp lệ, cả hai đều được thêm vào `matches`. `MergeSystem` dùng key `pieceInstanceId + subCellId` để tránh một subcell bị thêm trùng nhiều lần trong cùng lượt check.

## Flow resolve chain

`GameManager.ResolveTurnRoutine()` là trung tâm xử lý chain.

### Pha 1: Khởi tạo queue

- `pending` chứa các ô cần kiểm tra.
- ban đầu chỉ enqueue `placedCoord`.
- `resolvedStates` dùng để tránh resolve lặp vô hạn cùng một trạng thái match.
- `maxIterations` là chốt an toàn chống loop vô hạn.

### Pha 2: Lấy một ô ra để resolve

Với mỗi `coord` trong queue:

1. Gọi `mergeSystem.TryGetTouchMatchesForPlacedPiece(coord, out matchedSubs)`.
2. Nếu không có match thì bỏ qua ô này.
3. Nếu có match, tạo `stateKey` từ tập subcell match hiện tại; nếu state này đã xử lý trước đó thì bỏ qua.

### Pha 3: Pulse trước khi collect

Tất cả `JellyPiece` có subcell xuất hiện trong `matchedSubs` sẽ được gom vào `touchedPieces`, rồi gọi `PlayPreCollectPulse()` để báo hiệu chuẩn bị thu thập.

### Pha 4: Xóa subcell

Với từng `MatchedSubCellData`:

1. `piece.RemoveSubCellById(subCellId)` xóa đúng subcell khỏi list.
2. `JellyPiece.RefreshVisual()` chạy ngay trong `RemoveSubCellById()` để dựng lại shape còn sống.
3. `goalSystem.CollectRemovedColor(color, 1)` tăng tiến độ goal theo màu.

### Pha 5: Tách piece rỗng và piece còn sống

Sau khi xóa xong:

- piece nào không còn subcell sẽ vào `emptiedPieces`,
- piece nào còn subcell sẽ vào `survivedPieces`.

`emptiedPieces` sẽ bị gỡ khỏi board và bay về `collectCenter` bằng `PlayCollectAndRemoveRoutine()`.

### Pha 6: Cascade check tiếp

Mỗi `survivedPiece` được enqueue lại cùng với 4 ô hàng xóm của nó. Đây là phần làm cho game hỗ trợ rule “subcell bị xóa xong thì jelly co/phình và có thể ăn tiếp ngay trong cùng lượt”. Nếu shape mới tạo ra tiếp xúc mới, lượt resolve hiện tại sẽ bắt được ngay, không phải chờ lượt đặt jelly sau [1].

### Pha 7: Kết thúc lượt

Khi queue rỗng hoặc chạm giới hạn an toàn:

- nếu goal đủ thì win,
- nếu board hết ô trống thì lose,
- ngược lại spawn jelly preview kế tiếp.

## Goal và điều kiện thắng

`GoalSystem` chỉ đếm các màu có trong `LevelGoalData.goals`. Khi một subcell bị remove, `CollectRemovedColor(color, 1)` cộng tiến độ nếu màu đó là mục tiêu. `CheckWinCondition()` duyệt toàn bộ goals và chỉ win khi mọi màu đều đạt số lượng yêu cầu [query].
