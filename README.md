# Jelly Field Clone Project

Đây là một prototype game puzzle nhỏ được làm bằng Unity. Người chơi kéo và thả các khối jelly lên board, tạo match giữa các sub-cell cùng màu, hoàn thành mục tiêu của level và vượt qua màn chơi.

## Tổng quan project

Game được xây dựng theo dạng board grid 2D.

- Người chơi kéo và thả một jelly piece lên một ô trống trên board.
- Mỗi jelly piece gồm nhiều sub-cell với màu sắc khác nhau.
- Khi các sub-cell cùng màu chạm nhau giữa hai piece liền kề, chúng sẽ bị xóa.
- Màu đã xóa sẽ được cộng vào tiến độ goal của level.
- Nếu hoàn thành toàn bộ goal, người chơi thắng.
- Nếu board đầy và không còn ô trống, người chơi thua.

## Flow gameplay chính

1. Khởi tạo dữ liệu level.
2. Cấu hình kích thước board theo level.
3. Spawn jelly piece tiếp theo.
4. Người chơi kéo và thả piece vào một ô trống.
5. Hệ thống kiểm tra các sub-cell cùng màu đang chạm nhau giữa các piece liền kề.
6. Các sub-cell match sẽ bị xóa.
7. Các jelly piece rỗng hoàn toàn sẽ được collect và xóa khỏi board.
8. Tiến độ goal được cập nhật.
9. Hệ thống tiếp tục kiểm tra chain reaction cho tới khi không còn match.
10. Nếu game chưa kết thúc, piece tiếp theo sẽ được spawn.

## Các system chính

### GameManager

Quản lý flow chính của game.

Chức năng:

- Khởi tạo level.
- Spawn và setup jelly piece.
- Resolve một lượt sau khi người chơi đặt piece.
- Kiểm tra điều kiện thắng và thua.
- Trigger hiệu ứng collect và reward(coin?) khi thắng.

### BoardManager

Quản lý board dạng lưới.

Chức năng:

- Tạo và xóa board.
- Chuyển đổi giữa grid coordinate và world coordinate.
- Đặt và xóa piece trên board.
- Kiểm tra ô trống.
- Lấy các piece lân cận hoặc các ô đang có piece.

### MergeSystem

Quản lý logic tìm match.

Chức năng:

- Kiểm tra piece vừa đặt với 4 piece xung quanh.
- So sánh các sub-cell giữa 2 piece liền kề.
- Xác định các cặp sub-cell chạm nhau hợp lệ dựa trên màu và rect.
- Trả về danh sách sub-cell match không bị trùng.

### GoalSystem

Quản lý mục tiêu của level.

Chức năng:

- Khởi tạo goal của level.
- Ghi nhận số lượng màu đã collect.
- Cập nhật tiến độ goal.
- Kiểm tra điều kiện thắng.

### JellyPiece

Đại diện cho một khối jelly trên board.

Chức năng:

- Lưu dữ liệu sub-cell.
- Ghi nhớ vị trí hiện tại trên board.
- Xóa sub-cell khi bị match.
- Cập nhật lại hình ảnh hiển thị.
- Reset trạng thái khi spawn hoặc despawn từ pool.

### JellyAnimation

Quản lý animation cho jelly piece.

Chức năng:

- Idle animation.
- Drag jiggle animation khi kéo.
- Landing animation khi thả xuống board.
- Pre-collect pulse trước khi bị xóa.
- Collect animation khi bay về điểm collect.
- Đưa jelly quay lại trạng thái gốc một cách mượt mà.

### JellyPopEffect

Quản lý particle effect khi jelly bị collect.

Chức năng:

- Phát particle với màu tương ứng.
- Tự động despawn sau khi effect kết thúc.
- Reset particle state để tái sử dụng bằng object pool.

## Rule của merge

Logic merge của game hoạt động ở mức sub-cell, không phải toàn bộ piece.

Hai sub-cell được tính là match hợp lệ khi:

- Chúng thuộc về 2 piece khác nhau đang nằm cạnh nhau.
- Chúng có cùng màu.
- Chúng có runtime layout hợp lệ.
- Rect của chúng chạm nhau đúng theo hướng đang kiểm tra.
- Phần overlap trên trục còn lại lớn hơn tolerance được cấu hình.

Khi có match:

- Các sub-cell hợp lệ sẽ bị xóa.
- Goal sẽ được cộng theo màu của sub-cell bị xóa.
- Piece nào rỗng hoàn toàn sẽ được collect và xóa khỏi board.
- Các piece còn lại sẽ tiếp tục được kiểm tra để tạo chain reaction.

## Giải thích animation

### Idle

Animation scale và rotation nhẹ để jelly có cảm giác mềm và sống động.

### Drag Jiggle

Khi người chơi kéo piece, jelly sẽ bị kéo giãn và nghiêng theo hướng di chuyển.

### Landing

Khi piece được đặt xuống board, nó sẽ có hiệu ứng squash và bounce.

### Pre-Collect Pulse

Trước khi bị xóa, các piece liên quan sẽ pulse nhẹ để báo hiệu chuẩn bị collect.

### Collect

Khi piece rỗng hoàn toàn, nó sẽ bay về tâm collect, hơi kéo giãn theo hướng bay rồi thu nhỏ lại.

### Pop Effect

Sau khi collect xong, particle effect với màu tương ứng sẽ được phát và tự trả về pool.

## Object Pooling

Project sử dụng object pooling cho các object gameplay và effect có thể tái sử dụng.

Lợi ích:

- Giảm chi phí Instantiate và Destroy khi runtime.
- Hạn chế tạo garbage không cần thiết.
- Giúp việc tái sử dụng piece và effect ổn định hơn.

Các object pooled sẽ reset trạng thái trong `OnSpawned()` và `OnDespawned()` để tránh giữ lại dữ liệu cũ từ lần sử dụng trước.

## Cách chạy project

1. Mở project bằng Unity.
2. Mở scene gameplay chính.
3. Nhấn Play trong Unity Editor.
4. Kéo và thả jelly piece lên board để chơi.

## Ghi chú

- Project được tách thành các system riêng cho board logic, merge detection, goal, animation và effect.
- Coroutines được dùng để xử lý flow resolve theo thời gian và animation.
- Object pooling được dùng để tối ưu hiệu năng runtime cho các object tái sử dụng.