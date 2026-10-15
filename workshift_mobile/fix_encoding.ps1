$c = Get-Content 'C:\Users\nguye\OneDrive\Desktop\DoAnTN\WorkShift\workshift_mobile\lib\views\orders\staff_order_detail_view.dart' -Raw
$enc = [System.Text.Encoding]::UTF8
[IO.File]::WriteAllText('C:\Users\nguye\OneDrive\Desktop\DoAnTN\WorkShift\workshift_mobile\lib\views\orders\staff_order_detail_view.dart', $c, $enc)