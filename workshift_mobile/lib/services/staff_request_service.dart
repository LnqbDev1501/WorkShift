import 'package:dio/dio.dart';
import '../models/staff_request_model.dart';
import 'api_client.dart';

/// Dịch vụ kết nối API Server cho các chức năng Request của Staff
class StaffRequestService {
  final ApiClient _apiClient = ApiClient();

  /// Lấy danh sách yêu cầu có phân trang, tìm kiếm & lọc
  Future<PagedStaffRequestModel> getRequests({
    String? reason,
    String? requestType,
    String? status,
    DateTime? fromDate,
    DateTime? toDate,
    int pageNumber = 1,
    int pageSize = 10,
  }) async {
    try {
      final queryParams = <String, dynamic>{
        'pageNumber': pageNumber,
        'pageSize': pageSize,
      };

      if (reason != null && reason.trim().isNotEmpty) {
        queryParams['reason'] = reason.trim();
      }
      if (requestType != null && requestType.trim().isNotEmpty && requestType != 'ALL') {
        queryParams['requestType'] = requestType.trim();
      }
      if (status != null && status.trim().isNotEmpty && status != 'ALL') {
        queryParams['status'] = status.trim();
      }
      if (fromDate != null) {
        queryParams['fromDate'] =
            '${fromDate.year.toString().padLeft(4, '0')}-${fromDate.month.toString().padLeft(2, '0')}-${fromDate.day.toString().padLeft(2, '0')}';
      }
      if (toDate != null) {
        queryParams['toDate'] =
            '${toDate.year.toString().padLeft(4, '0')}-${toDate.month.toString().padLeft(2, '0')}-${toDate.day.toString().padLeft(2, '0')}';
      }

      final response = await _apiClient.get(
        '/api/staff/requests',
        queryParameters: queryParams,
      );

      if (response.data != null && response.data is Map<String, dynamic>) {
        return PagedStaffRequestModel.fromJson(response.data as Map<String, dynamic>);
      }

      return PagedStaffRequestModel(
        items: [],
        pageNumber: pageNumber,
        pageSize: pageSize,
        totalItems: 0,
        totalPages: 0,
      );
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Không thể tải danh sách yêu cầu');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Lấy chi tiết yêu cầu theo ID
  Future<StaffEmployeeRequestDetailModel> getRequestDetail(int requestId) async {
    try {
      final response = await _apiClient.get('/api/staff/requests/$requestId');
      if (response.data != null && response.data is Map<String, dynamic>) {
        return StaffEmployeeRequestDetailModel.fromJson(response.data as Map<String, dynamic>);
      }
      throw 'Không tìm thấy dữ liệu chi tiết yêu cầu.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Không thể tải chi tiết yêu cầu');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Lấy danh sách ca làm việc đủ điều kiện tạo đơn (LEAVE, LATE_ARRIVAL, EARLY_LEAVE)
  Future<List<StaffEligibleShiftModel>> getEligibleShifts(String requestType) async {
    try {
      final response = await _apiClient.get(
        '/api/staff/requests/eligible-shifts',
        queryParameters: {'requestType': requestType},
      );

      if (response.data != null && response.data is List) {
        return (response.data as List)
            .map((e) => StaffEligibleShiftModel.fromJson(e as Map<String, dynamic>))
            .toList();
      }
      return [];
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Không thể lấy danh sách ca làm việc đủ điều kiện');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Gửi đơn xin nghỉ phép (LEAVE)
  Future<StaffEmployeeRequestDetailModel> submitLeaveRequest({
    required List<int> assignmentIds,
    required String reason,
  }) async {
    try {
      final response = await _apiClient.post(
        '/api/staff/requests/leave',
        data: {
          'assignmentIds': assignmentIds,
          'reason': reason.trim(),
        },
      );

      if (response.data != null && response.data is Map<String, dynamic>) {
        return StaffEmployeeRequestDetailModel.fromJson(response.data as Map<String, dynamic>);
      }
      throw 'Không nhận được dữ liệu phản hồi từ máy chủ.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Gửi đơn nghỉ phép thất bại');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Gửi đơn xin đi trễ (LATE_ARRIVAL)
  Future<StaffEmployeeRequestDetailModel> submitLateArrivalRequest({
    required int assignmentId,
    required DateTime requestedTime,
    required String reason,
  }) async {
    try {
      final response = await _apiClient.post(
        '/api/staff/requests/late-arrival',
        data: {
          'assignmentId': assignmentId,
          'requestedTime': requestedTime.toUtc().toIso8601String(),
          'reason': reason.trim(),
        },
      );

      if (response.data != null && response.data is Map<String, dynamic>) {
        return StaffEmployeeRequestDetailModel.fromJson(response.data as Map<String, dynamic>);
      }
      throw 'Không nhận được dữ liệu phản hồi từ máy chủ.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Gửi đơn đi trễ thất bại');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Gửi đơn xin về sớm (EARLY_LEAVE)
  Future<StaffEmployeeRequestDetailModel> submitEarlyLeaveRequest({
    required int assignmentId,
    required DateTime requestedTime,
    required String reason,
  }) async {
    try {
      final response = await _apiClient.post(
        '/api/staff/requests/early-leave',
        data: {
          'assignmentId': assignmentId,
          'requestedTime': requestedTime.toUtc().toIso8601String(),
          'reason': reason.trim(),
        },
      );

      if (response.data != null && response.data is Map<String, dynamic>) {
        return StaffEmployeeRequestDetailModel.fromJson(response.data as Map<String, dynamic>);
      }
      throw 'Không nhận được dữ liệu phản hồi từ máy chủ.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Gửi đơn về sớm thất bại');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Hủy yêu cầu (Cancel Employee Request)
  Future<StaffEmployeeRequestDetailModel> cancelRequest(int requestId) async {
    try {
      final response = await _apiClient.post(
        '/api/staff/requests/$requestId/cancel',
      );

      if (response.data != null && response.data is Map<String, dynamic>) {
        return StaffEmployeeRequestDetailModel.fromJson(response.data as Map<String, dynamic>);
      }
      throw 'Hủy đơn thất bại. Vui lòng thử lại sau.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Hủy yêu cầu thất bại');
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
      if (data['errors'] != null && data['errors'] is Map) {
        final errors = data['errors'] as Map;
        final firstList = errors.values.firstWhere((v) => v is List && v.isNotEmpty, orElse: () => null);
        if (firstList != null && firstList is List && firstList.isNotEmpty) {
          return firstList.first.toString();
        }
      }
    }
    return e.message ?? defaultMessage;
  }
}
