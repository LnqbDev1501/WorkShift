/// Thông tin tài khoản nhân viên (khớp với ProfileResponse từ Backend .NET)
class UserModel {
  final String userId;
  final String fullName;
  final String email;
  final String phone;
  final String identityNo;
  final String gender;
  final String? dateOfBirth;
  final String address;
  final String avatarUrl;
  final String roleName;
  final String? branchName;
  final String? branchCode;
  final String? employeeTypeName;
  final String status;
  final String createdAt;
  final String updatedAt;

  UserModel({
    required this.userId,
    required this.fullName,
    required this.email,
    required this.phone,
    required this.identityNo,
    required this.gender,
    this.dateOfBirth,
    required this.address,
    required this.avatarUrl,
    required this.roleName,
    this.branchName,
    this.branchCode,
    this.employeeTypeName,
    required this.status,
    required this.createdAt,
    required this.updatedAt,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      userId: json['userId']?.toString() ?? '',
      fullName: json['fullName']?.toString() ?? '',
      email: json['email']?.toString() ?? '',
      phone: json['phone']?.toString() ?? '',
      identityNo: json['identityNo']?.toString() ?? '',
      gender: json['gender']?.toString() ?? '',
      dateOfBirth: json['dateOfBirth']?.toString(),
      address: json['address']?.toString() ?? '',
      avatarUrl: json['avatarUrl']?.toString() ?? '',
      roleName: json['roleName']?.toString() ?? '',
      branchName: json['branchName']?.toString(),
      branchCode: json['branchCode']?.toString(),
      employeeTypeName: json['employeeTypeName']?.toString(),
      status: json['status']?.toString() ?? 'ACTIVE',
      createdAt: json['createdAt']?.toString() ?? '',
      updatedAt: json['updatedAt']?.toString() ?? '',
    );
  }
}
