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
      if (data['detail'] != null && data['detail'].toString().isNotEmpty) {
        return _localizeErrorMessage(data['detail'].toString());
      }
      if (data['message'] != null && data['message'].toString().isNotEmpty) {
        return _localizeErrorMessage(data['message'].toString());
      }
      if (data['title'] != null && data['title'].toString().isNotEmpty) {
        return _localizeErrorMessage(data['title'].toString());
      }
      if (data['errors'] != null && data['errors'] is Map) {
        final errors = data['errors'] as Map;
        final firstList = errors.values.firstWhere((v) => v is List && v.isNotEmpty, orElse: () => null);
        if (firstList != null && firstList is List && firstList.isNotEmpty) {
          return _localizeErrorMessage(firstList.first.toString());
        }
      }
    }
    return _localizeErrorMessage(e.message ?? defaultMessage);
  }

  String _localizeErrorMessage(String msg) {
    if (msg.isEmpty) return msg;

    // Pattern: Requested (late arrival|early leave) time (2026-10-27 15:03:00 UTC) must be between shift start (2026-10-26 23:00:00 UTC) and shift end (2026-10-27 04:30:00 UTC).
    final timeRegex = RegExp(
      r'Requested (late arrival|early leave) time \(([^)]+)\) must be between shift start \(([^)]+)\) and shift end \(([^)]+)\)',
      caseSensitive: false,
    );
    final timeMatch = timeRegex.firstMatch(msg);
    if (timeMatch != null) {
      final isLate = (timeMatch.group(1) ?? '').toLowerCase() == 'late arrival';
      final reqStr = (timeMatch.group(2) ?? '').replaceAll(' UTC', '').trim();
      final startStr = (timeMatch.group(3) ?? '').replaceAll(' UTC', '').trim();
      final endStr = (timeMatch.group(4) ?? '').replaceAll(' UTC', '').trim();

      String formatUtcToVn(String str) {
        try {
          final dt = DateTime.parse(str).toLocal();
          final h = dt.hour.toString().padLeft(2, '0');
          final m = dt.minute.toString().padLeft(2, '0');
          final d = dt.day.toString().padLeft(2, '0');
          final mo = dt.month.toString().padLeft(2, '0');
          final y = dt.year.toString();
          return '$h:$m $d/$mo/$y';
        } catch (_) {
          return str;
        }
      }

      final reqVn = formatUtcToVn(reqStr);
      final startVn = formatUtcToVn(startStr);
      final endVn = formatUtcToVn(endStr);
      final action = isLate ? 'xin đi trễ' : 'xin về sớm';
      return 'Thời gian $action ($reqVn) không hợp lệ! Phải nằm trong khung giờ của ca làm việc: từ $startVn đến $endVn.';
    }

    final advanceRegex = RegExp(r'must be submitted at least (\d+) day\(s\) in advance', caseSensitive: false);
    final advanceMatch = advanceRegex.firstMatch(msg);
    if (advanceMatch != null) {
      final days = advanceMatch.group(1);
      return 'Đơn yêu cầu phải được gửi trước ít nhất $days ngày theo quy định chi nhánh.';
    }

    if (msg.toLowerCase().contains('monthly limit') || msg.toLowerCase().contains('limit of')) {
      return 'Bạn đã đạt giới hạn tối đa số đơn được gửi cho loại yêu cầu này trong tháng.';
    }

    if (msg.toLowerCase().contains('has already started or ended')) {
      return 'Ca làm việc này đã bắt đầu hoặc đã kết thúc, không thể gửi yêu cầu.';
    }

    if (msg.toLowerCase().contains('already exists for assignment')) {
      return 'Bạn đã có một đơn yêu cầu khác đang chờ duyệt hoặc đã duyệt cho ca làm này.';
    }

    return msg;
  }
}
