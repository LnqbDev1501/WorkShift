import 'dart:typed_data';
import 'package:dio/dio.dart';
import '../models/staff_contract_model.dart';
import 'api_client.dart';

/// Dịch vụ kết nối API Server cho các chức năng Hợp đồng lao động của Staff
class StaffContractService {
  final ApiClient _apiClient = ApiClient();

  /// Lấy danh sách hợp đồng của nhân viên (hỗ trợ tìm kiếm & lọc)
  Future<List<StaffContractListItemModel>> getMyContracts({
    String? status,
    String? search,
    DateTime? startDateFrom,
    DateTime? startDateTo,
  }) async {
    try {
      final queryParams = <String, dynamic>{};
      if (status != null && status.trim().isNotEmpty && status != 'ALL') {
        queryParams['status'] = status.trim();
      }
      if (search != null && search.trim().isNotEmpty) {
        queryParams['search'] = search.trim();
      }
      if (startDateFrom != null) {
        queryParams['startDateFrom'] =
            '${startDateFrom.year.toString().padLeft(4, '0')}-${startDateFrom.month.toString().padLeft(2, '0')}-${startDateFrom.day.toString().padLeft(2, '0')}';
      }
      if (startDateTo != null) {
        queryParams['startDateTo'] =
            '${startDateTo.year.toString().padLeft(4, '0')}-${startDateTo.month.toString().padLeft(2, '0')}-${startDateTo.day.toString().padLeft(2, '0')}';
      }

      final response = await _apiClient.get(
        '/api/staff/contracts',
        queryParameters: queryParams.isNotEmpty ? queryParams : null,
      );

      if (response.data != null && response.data is List) {
        return (response.data as List)
            .map((e) => StaffContractListItemModel.fromJson(e as Map<String, dynamic>))
            .toList();
      }
      return [];
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Không thể tải danh sách hợp đồng lao động');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Lấy chi tiết hợp đồng lao động theo ID
  Future<StaffContractDetailModel> getMyContractById(int contractId) async {
    try {
      final response = await _apiClient.get('/api/staff/contracts/$contractId');
      if (response.data != null && response.data is Map<String, dynamic>) {
        return StaffContractDetailModel.fromJson(response.data as Map<String, dynamic>);
      }
      throw 'Không tìm thấy dữ liệu hợp đồng.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Không thể tải chi tiết hợp đồng');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Chấp nhận / Ký hợp đồng lao động (Accept Employment Contract)
  Future<StaffContractDetailModel> acceptContract(int contractId) async {
    try {
      final response = await _apiClient.post('/api/staff/contracts/$contractId/accept');
      if (response.data != null && response.data is Map<String, dynamic>) {
        return StaffContractDetailModel.fromJson(response.data as Map<String, dynamic>);
      }
      throw 'Không nhận được phản hồi từ máy chủ.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Ký hợp đồng thất bại');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Gửi yêu cầu thay đổi / điều chỉnh thông tin hợp đồng (Submit Contract Change Request)
  Future<StaffContractChangeRequestModel> submitChangeRequest({
    required int contractId,
    String? staffComment,
    required List<ContractChangeRequestItemInputModel> items,
  }) async {
    try {
      final response = await _apiClient.post(
        '/api/staff/contracts/$contractId/change-requests',
        data: {
          'requestType': 'CHANGE_REQUEST',
          if (staffComment != null && staffComment.trim().isNotEmpty)
            'staffComment': staffComment.trim(),
          'items': items.map((e) => e.toJson()).toList(),
        },
      );

      if (response.data != null && response.data is Map<String, dynamic>) {
        return StaffContractChangeRequestModel.fromJson(response.data as Map<String, dynamic>);
      }
      throw 'Không nhận được phản hồi từ máy chủ.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Gửi yêu cầu thay đổi hợp đồng thất bại');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Lấy danh sách các yêu cầu thay đổi của hợp đồng (GET /api/staff/contracts/{id}/change-requests)
  Future<List<StaffContractChangeRequestModel>> getMyContractChangeRequests(int contractId) async {
    try {
      final response = await _apiClient.get('/api/staff/contracts/$contractId/change-requests');
      if (response.data != null && response.data is List) {
        return (response.data as List)
            .map((e) => StaffContractChangeRequestModel.fromJson(e as Map<String, dynamic>))
            .toList();
      }
      return [];
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Không thể tải lịch sử yêu cầu thay đổi');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Lấy dữ liệu file PDF (binary bytes) để xem trước hoặc tải về
  Future<Uint8List> getPdfBytes(int contractId, {bool isDownload = false}) async {
    try {
      final endpoint = isDownload
          ? '/api/staff/contracts/$contractId/pdf/download'
          : '/api/staff/contracts/$contractId/pdf';

      final response = await _apiClient.dio.get<List<int>>(
        endpoint,
        options: Options(responseType: ResponseType.bytes),
      );

      if (response.data != null) {
        return Uint8List.fromList(response.data!);
      }
      throw 'Không tải được tệp PDF.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Không thể tải tệp PDF của hợp đồng');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Lấy danh sách vị trí công việc hoạt động từ hệ thống (DB)
  Future<List<String>> getActivePositions() async {
    try {
      final response = await _apiClient.get('/api/staff/contracts/active-positions');
      if (response.data != null && response.data is List) {
        return (response.data as List)
            .map((e) => e.toString().trim())
            .where((s) => s.isNotEmpty)
            .toList();
      }
      return [];
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Không thể tải danh sách vị trí công việc');
    } catch (e) {
      throw e.toString();
    }
  }

  String _extractErrorMessage(DioException e, String defaultMessage) {
    if (e.response?.data != null && e.response!.data is Map<String, dynamic>) {
      final data = e.response!.data as Map<String, dynamic>;
      if (data['message'] != null && data['message'].toString().isNotEmpty) {
        return data['message'].toString();
      }
      if (data['title'] != null && data['title'].toString().isNotEmpty) {
        return data['title'].toString();
      }
    }
    return e.message ?? defaultMessage;
  }
}
