import 'dart:developer';
import 'package:dio/dio.dart';
import '../models/payroll_preview_model.dart';
import '../models/payslip_model.dart';
import 'api_client.dart';

class PayrollService {
  final ApiClient _apiClient = ApiClient();

  /// Helper to extract clean error message from ProblemDetails or standard response
  String _extractErrorMessage(DioException e, String fallback) {
    if (e.response?.data != null) {
      final data = e.response!.data;
      if (data is Map<String, dynamic>) {
        if (data['detail'] != null && data['detail'].toString().isNotEmpty) {
          return data['detail'].toString();
        }
        if (data['message'] != null && data['message'].toString().isNotEmpty) {
          return data['message'].toString();
        }
        if (data['title'] != null && data['title'].toString().isNotEmpty) {
          return data['title'].toString();
        }
      } else if (data is String && data.isNotEmpty) {
        return data;
      }
    }
    return e.message ?? fallback;
  }

  /// 1. Get payroll preview list (GET /api/payroll-previews)
  Future<List<PayrollPreviewItem>> getPayrollPreviews({
    int page = 1,
    int pageSize = 20,
    String? status,
    String? reviewStatus,
    int? payrollPeriodId,
    String? keyword,
  }) async {
    try {
      final queryParams = <String, dynamic>{'page': page, 'pageSize': pageSize};
      if (status != null && status.isNotEmpty) {
        queryParams['status'] = status;
      }
      if (reviewStatus != null && reviewStatus.isNotEmpty) {
        queryParams['reviewStatus'] = reviewStatus;
      }
      if (payrollPeriodId != null && payrollPeriodId > 0) {
        queryParams['payrollPeriodId'] = payrollPeriodId;
      }
      if (keyword != null && keyword.trim().isNotEmpty) {
        queryParams['keyword'] = keyword.trim();
      }

      final response = await _apiClient.get(
        '/api/payroll-previews',
        queryParameters: queryParams,
      );

      if (response.statusCode == 200 && response.data != null) {
        final data = response.data;
        List<dynamic> items = [];
        if (data is Map<String, dynamic> && data['items'] is List) {
          items = data['items'] as List<dynamic>;
        } else if (data is List) {
          items = data;
        }
        return items
            .map(
              (item) =>
                  PayrollPreviewItem.fromJson(item as Map<String, dynamic>),
            )
            .toList();
      }
      return [];
    } on DioException catch (e) {
      final msg = _extractErrorMessage(
        e,
        'Unable to load payroll preview list',
      );
      log('PayrollService.getPayrollPreviews error: $msg');
      throw msg;
    } catch (e) {
      log('PayrollService.getPayrollPreviews error: $e');
      rethrow;
    }
  }

  /// 2. Get payroll preview details (GET /api/payroll-previews/{id})
  Future<PayrollPreviewDetail> getPayrollPreviewDetail(int feedbackId) async {
    try {
      final response = await _apiClient.get(
        '/api/payroll-previews/$feedbackId',
      );

      if (response.statusCode == 200 && response.data != null) {
        return PayrollPreviewDetail.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'Payroll preview details not found.';
    } on DioException catch (e) {
      final msg = _extractErrorMessage(
        e,
        'Unable to load payroll preview details',
      );
      log('PayrollService.getPayrollPreviewDetail error: $msg');
      throw msg;
    } catch (e) {
      log('PayrollService.getPayrollPreviewDetail error: $e');
      rethrow;
    }
  }

  /// 3. Confirm payroll preview (POST /api/payroll-previews/{id}/confirm)
  Future<PayrollPreviewItem> confirmPayrollPreview(int feedbackId) async {
    try {
      final response = await _apiClient.post(
        '/api/payroll-previews/$feedbackId/confirm',
      );

      if (response.statusCode == 200 && response.data != null) {
        return PayrollPreviewItem.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'Unable to confirm payroll.';
    } on DioException catch (e) {
      final msg = _extractErrorMessage(e, 'Payroll confirmation failed');
      log('PayrollService.confirmPayrollPreview error: $msg');
      throw msg;
    } catch (e) {
      log('PayrollService.confirmPayrollPreview error: $e');
      rethrow;
    }
  }

  /// 4. Submit payroll feedback (POST /api/payroll-previews/{id}/feedback)
  Future<PayrollPreviewItem> submitPayrollFeedback(
    int feedbackId,
    String description,
  ) async {
    try {
      final response = await _apiClient.post(
        '/api/payroll-previews/$feedbackId/feedback',
        data: {'description': description.trim()},
      );

      if (response.statusCode == 200 && response.data != null) {
        return PayrollPreviewItem.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'Unable to submit feedback.';
    } on DioException catch (e) {
      final msg = _extractErrorMessage(e, 'Failed to submit feedback');
      log('PayrollService.submitPayrollFeedback error: $msg');
      throw msg;
    } catch (e) {
      log('PayrollService.submitPayrollFeedback error: $e');
      rethrow;
    }
  }

  /// 5. Get submitted staff feedbacks (GET /api/payroll-previews)
  Future<List<PayrollPreviewItem>> getMyPayrollFeedbacks({
    int page = 1,
    int pageSize = 20,
    String? status, // SUBMITTED, APPROVED, EXPLAINED
    String? keyword,
  }) async {
    try {
      final queryParams = <String, dynamic>{'page': page, 'pageSize': pageSize};
      if (status != null && status.isNotEmpty) {
        queryParams['status'] = status;
      }
      if (keyword != null && keyword.trim().isNotEmpty) {
        queryParams['keyword'] = keyword.trim();
      }

      final response = await _apiClient.get(
        '/api/payroll-previews',
        queryParameters: queryParams,
      );

      if (response.statusCode == 200 && response.data != null) {
        final data = response.data;
        List<dynamic> items = [];
        if (data is Map<String, dynamic> && data['items'] is List) {
          items = data['items'] as List<dynamic>;
        } else if (data is List) {
          items = data;
        }

        final list = items
            .map(
              (item) =>
                  PayrollPreviewItem.fromJson(item as Map<String, dynamic>),
            )
            .toList();

        // Filter review records that actually have staff feedback if no specific status is requested
        if (status == null || status.isEmpty) {
          return list
              .where(
                (f) =>
                    f.status != null ||
                    (f.description != null && f.description!.isNotEmpty) ||
                    f.reviewStatus == PayrollReviewStatus.feedbackPending,
              )
              .toList();
        }
        return list;
      }
      return [];
    } on DioException catch (e) {
      final msg = _extractErrorMessage(e, 'Unable to load feedback list');
      log('PayrollService.getMyPayrollFeedbacks error: $msg');
      throw msg;
    } catch (e) {
      log('PayrollService.getMyPayrollFeedbacks error: $e');
      rethrow;
    }
  }

  /// 6. Get official payslips list (GET /api/payslips)
  Future<List<PayslipSummaryItem>> getPayslips({
    int page = 1,
    int pageSize = 20,
    String? keyword,
    String? status,
  }) async {
    try {
      final queryParams = <String, dynamic>{'page': page, 'pageSize': pageSize};
      if (keyword != null && keyword.trim().isNotEmpty) {
        queryParams['keyword'] = keyword.trim();
      }
      if (status != null && status.isNotEmpty) {
        queryParams['status'] = status;
      }

      final response = await _apiClient.get(
        '/api/payslips',
        queryParameters: queryParams,
      );

      if (response.statusCode == 200 && response.data != null) {
        final data = response.data;
        List<dynamic> items = [];
        if (data is Map<String, dynamic> && data['items'] is List) {
          items = data['items'] as List<dynamic>;
        } else if (data is List) {
          items = data;
        }
        return items
            .map(
              (item) =>
                  PayslipSummaryItem.fromJson(item as Map<String, dynamic>),
            )
            .toList();
      }
      return [];
    } on DioException catch (e) {
      final msg = _extractErrorMessage(e, 'Unable to load payslips list');
      log('PayrollService.getPayslips error: $msg');
      throw msg;
    } catch (e) {
      log('PayrollService.getPayslips error: $e');
      rethrow;
    }
  }

  /// 7. Get official payslip details (GET /api/payslips/{id})
  Future<PayslipDetailModel> getPayslipDetail(int payslipId) async {
    try {
      final response = await _apiClient.get('/api/payslips/$payslipId');

      if (response.statusCode == 200 && response.data != null) {
        return PayslipDetailModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      throw 'Payslip details not found.';
    } on DioException catch (e) {
      final msg = _extractErrorMessage(e, 'Unable to load payslip details');
      log('PayrollService.getPayslipDetail error: $msg');
      throw msg;
    } catch (e) {
      log('PayrollService.getPayslipDetail error: $e');
      rethrow;
    }
  }
}
