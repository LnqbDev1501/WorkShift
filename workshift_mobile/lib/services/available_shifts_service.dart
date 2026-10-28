import '../utils/api_date_format.dart';
import '../models/available_shift_model.dart';
import 'api_client.dart';

class AvailableShiftsService {
  final ApiClient _apiClient;

  AvailableShiftsService({ApiClient? apiClient})
    : _apiClient = apiClient ?? ApiClient();

  Future<AvailableShiftPage> getAvailableShifts({
    int pageNumber = 1,
    int pageSize = 20,
    DateTime? fromDate,
    DateTime? toDate,
  }) async {
    final response = await _apiClient.get(
      '/api/staff/shifts/available',
      queryParameters: {
        'PageNumber': pageNumber,
        'PageSize': pageSize,
        if (fromDate != null) 'FromDate': formatApiDate(fromDate),
        if (toDate != null) 'ToDate': formatApiDate(toDate),
      },
    );
    return AvailableShiftPage.fromJson(response.data as Map<String, dynamic>);
  }
}
