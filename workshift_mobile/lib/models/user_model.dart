/// Thông tin tài khoản nhân viên (khớp với ProfileResponse từ Backend .NET)
class UserModel {
  final String userId;
  final String fullName;
  final String email;
  final String phone;
  final String avatarUrl;
  final String roleName;
  final String? branchName;

  UserModel({
    required this.userId,
    required this.fullName,
    required this.email,
    required this.phone,
    required this.avatarUrl,
    required this.roleName,
    this.branchName,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      userId: json['userId']?.toString() ?? '',
      fullName: json['fullName']?.toString() ?? '',
      email: json['email']?.toString() ?? '',
      phone: json['phone']?.toString() ?? '',
      avatarUrl: json['avatarUrl']?.toString() ?? '',
      roleName: json['roleName']?.toString() ?? '',
      branchName: json['branchName']?.toString(),
    );
  }
}
