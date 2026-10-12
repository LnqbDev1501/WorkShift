import 'package:flutter/material.dart';

/// Bảng màu chuẩn của ứng dụng WorkShift theo thiết kế thực tế
class AppColors {
  // Màu thương hiệu chính (Primary - Xanh dương phong cách hiện đại)
  static const Color primary = Color(0xFF2563EB); // Màu nút "Chấm công vào", logo WorkShift
  static const Color primaryDark = Color(0xFF1D4ED8);
  static const Color primaryLight = Color(0xFFEFF6FF); // Nền xanh nhạt cho chip, highlight
  
  // Màu nền ứng dụng
  static const Color background = Color(0xFFF8FAFC); // Nền xám nhạt toàn app
  static const Color surface = Color(0xFFFFFFFF);    // Nền trắng của các Card/Thẻ
  
  // Màu chữ (Typography)
  static const Color textPrimary = Color(0xFF0F172A);   // Đen đậm cho tiêu đề (Chào buổi sáng, Minh!)
  static const Color textSecondary = Color(0xFF475569); // Xám đậm cho nội dung (Ca sáng • Pha chế)
  static const Color textMuted = Color(0xFF94A3B8);     // Xám nhạt cho ngày tháng, ghi chú nhỏ
  
  // Màu đường viền & ngăn cách
  static const Color border = Color(0xFFE2E8F0);
  static const Color borderLight = Color(0xFFF1F5F9);
  
  // Màu trạng thái cảnh báo & Thông báo
  static const Color warning = Color(0xFFEA580C);       // Cam cảnh báo (Sắp đến ca)
  static const Color warningBg = Color(0xFFFFEDD5);     // Nền cam nhạt
  
  static const Color success = Color(0xFF10B981);       // Xanh lá thành công (Đã duyệt, trực ca)
  static const Color successBg = Color(0xFFD1FAE5);     // Nền xanh lá nhạt
  
  static const Color error = Color(0xFFEF4444);         // Đỏ lỗi / Badge số lượng cần xử lý
  static const Color errorBg = Color(0xFFFEE2E2);       // Nền đỏ nhạt

  static const Color info = Color(0xFF0284C7);
  static const Color infoBg = Color(0xFFE0F2FE);

  // Màu cho 4 nút Thao tác nhanh (Quick actions)
  static const Color actionOrderBg = Color(0xFFDBEAFE);      // Tạo đơn hàng (xanh biển nhạt)
  static const Color actionOrderIcon = Color(0xFF2563EB);
  
  static const Color actionRegisterBg = Color(0xFFE0E7FF);   // Đăng ký ca (tím indigo nhạt)
  static const Color actionRegisterIcon = Color(0xFF4F46E5);
  
  static const Color actionSwapBg = Color(0xFFFFEDD5);       // Đổi ca (cam nhạt)
  static const Color actionSwapIcon = Color(0xFFEA580C);
  
  static const Color actionLeaveBg = Color(0xFFD1FAE5);      // Gửi yêu cầu (xanh mint nhạt)
  static const Color actionLeaveIcon = Color(0xFF059669);
}
