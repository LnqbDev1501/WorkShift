import '../../models/adjustments/employee_adjustment_model.dart';
import '../core/api_client.dart';

class StaffAdjustmentService {
  final ApiClient _apiClient = ApiClient();

  /// Lấy danh sách khen thưởng của chính Staff
  Future<EmployeeAdjustmentPagedResponse> getRewards({
    int page = 1,
    int pageSize = 20,
    String? status,
  }) async {
    final query = <String, dynamic>{'page': page, 'pageSize': pageSize};
    if (status != null && status.isNotEmpty && status.toUpperCase() != 'ALL') {
      query['status'] = status.toUpperCase();
    }

    final response = await _apiClient.get(
      '/api/staff/rewards',
      queryParameters: query,
    );
    return EmployeeAdjustmentPagedResponse.fromJson(
      response.data as Map<String, dynamic>,
    );
  }

  /// Lấy chi tiết khen thưởng của chính Staff
  Future<EmployeeAdjustmentModel> getRewardDetail(int id) async {
    final response = await _apiClient.get('/api/staff/rewards/$id');
    return EmployeeAdjustmentModel.fromJson(
      response.data as Map<String, dynamic>,
    );
  }

  /// Lấy danh sách kỷ luật / phạt của chính Staff
  Future<EmployeeAdjustmentPagedResponse> getPenalties({
    int page = 1,
    int pageSize = 20,
    String? status,
  }) async {
    final query = <String, dynamic>{'page': page, 'pageSize': pageSize};
    if (status != null && status.isNotEmpty && status.toUpperCase() != 'ALL') {
      query['status'] = status.toUpperCase();
    }

    final response = await _apiClient.get(
      '/api/staff/penalties',
      queryParameters: query,
    );
    return EmployeeAdjustmentPagedResponse.fromJson(
      response.data as Map<String, dynamic>,
    );
  }

  /// Lấy chi tiết kỷ luật / phạt của chính Staff
  Future<EmployeeAdjustmentModel> getPenaltyDetail(int id) async {
    final response = await _apiClient.get('/api/staff/penalties/$id');
    return EmployeeAdjustmentModel.fromJson(
      response.data as Map<String, dynamic>,
    );
  }
}
