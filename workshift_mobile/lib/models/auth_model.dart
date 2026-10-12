/// Model dữ liệu gửi lên khi đăng nhập (khớp với LoginRequest của Backend .NET)
class LoginRequest {
  final String email;
  final String password;

  LoginRequest({
    required this.email,
    required this.password,
  });

  Map<String, dynamic> toJson() {
    return {
      'email': email.trim(),
      'password': password.trim(),
    };
  }
}

/// Model dữ liệu nhận về sau khi đăng nhập thành công (khớp với LoginResponse của Backend .NET)
class LoginResponse {
  final String accessToken;
  final String refreshToken;
  final int expiresIn;
  final String userId;
  final String email;
  final String role;
  final String fullName;

  LoginResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
    required this.userId,
    required this.email,
    required this.role,
    required this.fullName,
  });

  factory LoginResponse.fromJson(Map<String, dynamic> json) {
    return LoginResponse(
      accessToken: json['accessToken']?.toString() ?? '',
      refreshToken: json['refreshToken']?.toString() ?? '',
      expiresIn: (json['expiresIn'] as num?)?.toInt() ?? 0,
      userId: json['userId']?.toString() ?? '',
      email: json['email']?.toString() ?? '',
      role: json['role']?.toString() ?? '',
      fullName: json['fullName']?.toString() ?? '',
    );
  }
}
