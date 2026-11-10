import 'package:flutter/material.dart';
import 'dart:typed_data';
import 'package:dio/dio.dart';
import '../../models/auth/user_model.dart';
import '../../services/core/api_client.dart';

/// Bộ não điều khiển màn hình Profile (thông tin cá nhân & đổi mật khẩu)
class ProfileController extends ChangeNotifier {
  final ApiClient _apiClient = ApiClient();

  UserModel? _user;
  bool _isLoading = false;
  bool _isChangingPassword = false;
  bool _isUpdatingProfile = false;
  bool _isUploadingAvatar = false;
  bool _isChangingEmail = false;
  String? _errorMessage;
  String? _successMessage;

  UserModel? get user => _user;
  bool get isLoading => _isLoading;
  bool get isChangingPassword => _isChangingPassword;
  bool get isUpdatingProfile => _isUpdatingProfile;
  bool get isUploadingAvatar => _isUploadingAvatar;
  bool get isChangingEmail => _isChangingEmail;
  String? get errorMessage => _errorMessage;
  String? get successMessage => _successMessage;

  /// Lấy thông tin profile của nhân viên: GET /api/profile/me
  Future<void> loadProfile() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiClient.get('/api/profile/me');
      if (response.statusCode == 200 && response.data != null) {
        _user = UserModel.fromJson(response.data as Map<String, dynamic>);
      }
    } catch (e) {
      _errorMessage = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Cập nhật thông tin cá nhân: PUT /api/profile/me
  Future<bool> updateProfile({
    required String fullName,
    required String phone,
    required String gender,
    String? dateOfBirth,
    required String address,
  }) async {
    final phoneRegex = RegExp(r'^(03[2-9]|05[689]|07[06-9]|08[1-689]|09[0134678])\d{7}$');
    if (!phoneRegex.hasMatch(phone.trim())) {
      _errorMessage = 'Invalid phone number. Please enter a valid 10-digit mobile number.';
      notifyListeners();
      return false;
    }

    _isUpdatingProfile = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiClient.put(
        '/api/profile/me',
        data: {
          'fullName': fullName.trim(),
          'phone': phone.trim(),
          'gender': gender.trim(),
          if (dateOfBirth != null && dateOfBirth.isNotEmpty)
            'dateOfBirth': dateOfBirth,
          'address': address.trim(),
        },
      );

      if (response.statusCode == 200 && response.data != null) {
        _user = UserModel.fromJson(response.data as Map<String, dynamic>);
        _successMessage = 'Personal information updated successfully.';
        _isUpdatingProfile = false;
        notifyListeners();
        return true;
      }
      _errorMessage = 'Unable to update personal information. Please try again.';
      return false;
    } on String catch (e) {
      _errorMessage = e;
      _isUpdatingProfile = false;
      notifyListeners();
      return false;
    } catch (e) {
      _errorMessage = e.toString();
      _isUpdatingProfile = false;
      notifyListeners();
      return false;
    }
  }

  /// Upload avatar: POST /api/profile/me/avatar
  /// imageBytes: Uint8List (works on both web and mobile)
  /// fileName: original file name
  Future<bool> uploadAvatar(Uint8List imageBytes, String fileName) async {
    _isUploadingAvatar = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final formData = FormData.fromMap({
        'avatarFile': MultipartFile.fromBytes(imageBytes, filename: fileName),
      });
      final response = await _apiClient.post(
        '/api/profile/me/avatar',
        data: formData,
      );

      if (response.statusCode == 200 && response.data != null) {
        final avatarUrl = response.data['avatarUrl']?.toString() ?? '';
        if (_user != null) {
          _user = UserModel(
            userId: _user!.userId,
            fullName: _user!.fullName,
            email: _user!.email,
            phone: _user!.phone,
            identityNo: _user!.identityNo,
            gender: _user!.gender,
            dateOfBirth: _user!.dateOfBirth,
            address: _user!.address,
            avatarUrl: avatarUrl,
            roleName: _user!.roleName,
            branchName: _user!.branchName,
            branchCode: _user!.branchCode,
            employeeTypeName: _user!.employeeTypeName,
            status: _user!.status,
            createdAt: _user!.createdAt,
            updatedAt: _user!.updatedAt,
          );
        }
        _successMessage = 'Profile picture updated successfully.';
        _isUploadingAvatar = false;
        notifyListeners();
        return true;
      }
      _errorMessage = 'Failed to upload profile picture. Please try again.';
      return false;
    } on String catch (e) {
      _errorMessage = e;
      _isUploadingAvatar = false;
      notifyListeners();
      return false;
    } catch (e) {
      _errorMessage = e.toString();
      _isUploadingAvatar = false;
      notifyListeners();
      return false;
    }
  }

  /// Yêu cầu đổi email: POST /api/profile/email/change-request
  Future<bool> requestEmailChange(String newEmail) async {
    _isChangingEmail = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiClient.post(
        '/api/profile/email/change-request',
        data: {'email': newEmail.trim()},
      );

      if (response.statusCode == 202 || response.statusCode == 200) {
        _successMessage =
            'A verification link has been sent to your new email.';
        _isChangingEmail = false;
        notifyListeners();
        return true;
      }
      _errorMessage = 'Unable to submit email change request. Please try again.';
      return false;
    } on String catch (e) {
      _errorMessage = e;
      _isChangingEmail = false;
      notifyListeners();
      return false;
    } catch (e) {
      _errorMessage = e.toString();
      _isChangingEmail = false;
      notifyListeners();
      return false;
    }
  }

  /// Đổi mật khẩu: PUT /api/profile/password
  Future<bool> changePassword({
    required String currentPassword,
    required String newPassword,
    required String confirmPassword,
  }) async {
    _isChangingPassword = true;
    _errorMessage = null;
    _successMessage = null;
    notifyListeners();

    try {
      final response = await _apiClient.put(
        '/api/profile/password',
        data: {
          'currentPassword': currentPassword.trim(),
          'password': newPassword.trim(),
        },
      );

      if (response.statusCode == 200 || response.statusCode == 204) {
        _successMessage = 'Password changed successfully.';
        _isChangingPassword = false;
        notifyListeners();
        return true;
      }
      _errorMessage = 'Unable to change password. Please try again.';
      return false;
    } on String catch (e) {
      _errorMessage = e;
      _isChangingPassword = false;
      notifyListeners();
      return false;
    } catch (e) {
      _errorMessage = e.toString();
      _isChangingPassword = false;
      notifyListeners();
      return false;
    }
  }

  /// Xóa thông báo lỗi
  void clearError() {
    if (_errorMessage != null) {
      _errorMessage = null;
      notifyListeners();
    }
  }

  /// Xóa thông báo thành công
  void clearSuccess() {
    if (_successMessage != null) {
      _successMessage = null;
      notifyListeners();
    }
  }
}
