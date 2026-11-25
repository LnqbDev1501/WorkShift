import '../../models/shifts/shift_registration_model.dart';
import '../core/api_client.dart';

class ShiftRegistrationService {
  final ApiClient _apiClient;

  ShiftRegistrationService({ApiClient? apiClient})
    : _apiClient = apiClient ?? ApiClient();

  Future<ShiftAccessModel> getAccess() async {
    try {
      final response = await _apiClient.get('/api/staff/shifts/access');
      return ShiftAccessModel.fromJson(response.data as Map<String, dynamic>);
    } catch (_) {
      return ShiftAccessModel.unavailable;
    }
  }

  Future<ShiftRegistrationModel> register(int shiftId) async {
    final response = await _apiClient.post(
      '/api/staff/shifts/$shiftId/register',
    );
    return ShiftRegistrationModel.fromJson(
      response.data as Map<String, dynamic>,
    );
  }

  Future<ShiftRegistrationModel> cancel(int shiftId, String reason) async {
    final response = await _apiClient.post(
      '/api/staff/shifts/$shiftId/cancel-registration',
      data: {'reason': reason},
    );
    return ShiftRegistrationModel.fromJson(
      response.data as Map<String, dynamic>,
    );
  }
}
