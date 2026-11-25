import 'dart:developer';
import 'package:dio/dio.dart';
import '../../models/timesheet/staff_timesheet_model.dart';
import '../core/api_client.dart';

class StaffTimesheetService {
  final ApiClient _apiClient = ApiClient();

  /// Retrieves the authenticated staff member's weekly timesheet: GET /api/staff/timesheets/my-timesheet
  Future<StaffWeeklyTimesheetModel?> getMyWeeklyTimesheet({
    int? scheduleWeekId,
  }) async {
    try {
      final queryParams = <String, dynamic>{};
      if (scheduleWeekId != null && scheduleWeekId > 0) {
        queryParams['scheduleWeekId'] = scheduleWeekId;
      }

      final response = await _apiClient.get(
        '/api/staff/timesheets/my-timesheet',
        queryParameters: queryParams.isNotEmpty ? queryParams : null,
      );

      if (response.statusCode == 200 && response.data != null) {
        return StaffWeeklyTimesheetModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      return null;
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) {
        return null;
      }
      final msg =
          e.response?.data?['message']?.toString() ??
          e.message ??
          'Failed to load timesheet';
      log('StaffTimesheetService error: $msg');
      throw msg;
    } catch (e) {
      log('StaffTimesheetService error: $e');
      rethrow;
    }
  }

  /// Confirms weekly timesheet: POST /api/staff/timesheets/confirm
  Future<StaffWeeklyTimesheetModel> confirmTimesheet({
    required int scheduleWeekId,
    String? note,
  }) async {
    try {
      final response = await _apiClient.post(
        '/api/staff/timesheets/confirm',
        data: {
          'scheduleWeekId': scheduleWeekId,
          if (note != null && note.trim().isNotEmpty) 'note': note.trim(),
        },
      );

      if (response.statusCode == 200 && response.data != null) {
        return StaffWeeklyTimesheetModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'Failed to confirm timesheet';
    } on DioException catch (e) {
      final msg =
          e.response?.data?['message']?.toString() ??
          e.message ??
          'Confirmation failed';
      log('StaffTimesheetService confirm error: $msg');
      throw msg;
    } catch (e) {
      log('StaffTimesheetService confirm error: $e');
      rethrow;
    }
  }

  /// Submits one or multiple timesheet adjustment requests: POST /api/staff/timesheets/adjustments
  Future<StaffWeeklyTimesheetModel> submitAdjustments({
    required int scheduleWeekId,
    required List<ShiftAdjustmentItemInput> adjustments,
  }) async {
    try {
      final response = await _apiClient.post(
        '/api/staff/timesheets/adjustments',
        data: {
          'scheduleWeekId': scheduleWeekId,
          'adjustments': adjustments.map((a) => a.toJson()).toList(),
        },
      );

      if (response.statusCode == 200 && response.data != null) {
        return StaffWeeklyTimesheetModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'Failed to submit timesheet adjustments';
    } on DioException catch (e) {
      final msg =
          e.response?.data?['message']?.toString() ??
          e.message ??
          'Submission failed';
      log('StaffTimesheetService adjustments error: $msg');
      throw msg;
    } catch (e) {
      log('StaffTimesheetService adjustments error: $e');
      rethrow;
    }
  }

  /// Cancels a pending timesheet adjustment request: POST /api/staff/timesheets/adjustments/{id}/cancel
  Future<StaffWeeklyTimesheetModel> cancelAdjustment({
    required int adjustmentRequestId,
  }) async {
    try {
      final response = await _apiClient.post(
        '/api/staff/timesheets/adjustments/$adjustmentRequestId/cancel',
      );

      if (response.statusCode == 200 && response.data != null) {
        return StaffWeeklyTimesheetModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'Failed to cancel timesheet adjustment';
    } on DioException catch (e) {
      final msg =
          e.response?.data?['message']?.toString() ??
          e.message ??
          'Cancellation failed';
      log('StaffTimesheetService cancel error: $msg');
      throw msg;
    } catch (e) {
      log('StaffTimesheetService cancel error: $e');
      rethrow;
    }
  }
}
