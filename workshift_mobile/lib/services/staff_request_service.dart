import '../utils/api_date_format.dart';
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
    bool sortByUpdatedAt = false,
  }) async {
    try {
      final queryParams = <String, dynamic>{
        'pageNumber': pageNumber,
        'pageSize': pageSize,
        if (sortByUpdatedAt) 'sortByUpdatedAt': true,
      };

      if (reason != null && reason.trim().isNotEmpty) {
        queryParams['reason'] = reason.trim();
      }
      if (requestType != null &&
          requestType.trim().isNotEmpty &&
          requestType != 'ALL') {
        queryParams['requestType'] = requestType.trim();
      }
      if (status != null && status.trim().isNotEmpty && status != 'ALL') {
        queryParams['status'] = status.trim();
      }
      if (fromDate != null) {
        queryParams['fromDate'] = formatApiDate(fromDate);
      }
      if (toDate != null) {
        queryParams['toDate'] = formatApiDate(toDate);
      }

      final response = await _apiClient.get(
        '/api/staff/requests',
        queryParameters: queryParams,
      );

      if (response.data != null && response.data is Map<String, dynamic>) {
        return PagedStaffRequestModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }

      return PagedStaffRequestModel(
        items: [],
        pageNumber: pageNumber,
        pageSize: pageSize,
        totalItems: 0,
        totalPages: 0,
      );
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Failed to load requests');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Lấy chi tiết yêu cầu theo ID
  Future<StaffEmployeeRequestDetailModel> getRequestDetail(
    int requestId,
  ) async {
    try {
      final response = await _apiClient.get('/api/staff/requests/$requestId');
      if (response.data != null && response.data is Map<String, dynamic>) {
        return StaffEmployeeRequestDetailModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'Request details not found.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Failed to load request details');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Lấy danh sách ca làm việc đủ điều kiện tạo đơn (LEAVE, LATE_ARRIVAL, EARLY_LEAVE)
  Future<List<StaffEligibleShiftModel>> getEligibleShifts(
    String requestType,
  ) async {
    try {
      final response = await _apiClient.get(
        '/api/staff/requests/eligible-shifts',
        queryParameters: {'requestType': requestType},
      );

      if (response.data != null && response.data is List) {
        return (response.data as List)
            .map(
              (e) =>
                  StaffEligibleShiftModel.fromJson(e as Map<String, dynamic>),
            )
            .toList();
      }
      return [];
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Failed to load eligible shifts');
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
        data: {'assignmentIds': assignmentIds, 'reason': reason.trim()},
      );

      if (response.data != null && response.data is Map<String, dynamic>) {
        return StaffEmployeeRequestDetailModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'No response received from server.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Failed to submit leave request');
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
        return StaffEmployeeRequestDetailModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'No response received from server.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Failed to submit late arrival request');
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
        return StaffEmployeeRequestDetailModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'No response received from server.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Failed to submit early departure request');
    } catch (e) {
      throw e.toString();
    }
  }

  /// Gửi đơn xin tăng lương (SALARY_INCREASE)
  Future<StaffEmployeeRequestDetailModel> submitSalaryIncreaseRequest({
    required double proposedHourlyRate,
    required String reason,
  }) async {
    try {
      final response = await _apiClient.post(
        '/api/staff/requests/salary-increase',
        data: {
          'proposedHourlyRate': proposedHourlyRate,
          'reason': reason.trim(),
        },
      );

      if (response.data != null && response.data is Map<String, dynamic>) {
        return StaffEmployeeRequestDetailModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'No response received from server.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Failed to submit salary increase request');
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
        return StaffEmployeeRequestDetailModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'Failed to cancel request. Please try again.';
    } on DioException catch (e) {
      throw _extractErrorMessage(e, 'Failed to cancel request');
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
        final firstList = errors.values.firstWhere(
          (v) => v is List && v.isNotEmpty,
          orElse: () => null,
        );
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

      String formatUtcToLocal(String str) {
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

      final reqLocal = formatUtcToLocal(reqStr);
      final startLocal = formatUtcToLocal(startStr);
      final endLocal = formatUtcToLocal(endStr);
      final action = isLate ? 'late arrival' : 'early departure';
      return 'The requested $action time ($reqLocal) is invalid! It must be within the shift schedule: from $startLocal to $endLocal.';
    }

    final advanceRegex = RegExp(
      r'must be submitted at least (\d+) day\(s\) in advance',
      caseSensitive: false,
    );
    final advanceMatch = advanceRegex.firstMatch(msg);
    if (advanceMatch != null) {
      final days = advanceMatch.group(1);
      return 'Requests must be submitted at least $days day(s) in advance according to branch policy.';
    }

    if (msg.toLowerCase().contains('monthly limit') ||
        msg.toLowerCase().contains('limit of')) {
      return 'You have reached the maximum monthly limit for this request type.';
    }

    if (msg.toLowerCase().contains('has already started or ended')) {
      return 'This shift has already started or ended. Request cannot be submitted.';
    }

    if (msg.toLowerCase().contains('already exists for assignment')) {
      return 'You already have an existing request for this shift assignment.';
    }

    return msg;
  }
}
