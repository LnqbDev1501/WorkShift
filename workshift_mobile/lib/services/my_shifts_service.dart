import '../utils/api_date_format.dart';
import '../models/available_shift_model.dart';
import 'api_client.dart';

class MyShiftsService {
  final ApiClient _apiClient;
  MyShiftsService({ApiClient? apiClient})
    : _apiClient = apiClient ?? ApiClient();

  Future<AvailableShiftPage> getMyShifts({
    int pageNumber = 1,
    required DateTime fromDate,
    DateTime? toDate,
  }) async {
    final response = await _apiClient.get(
      '/api/staff/shifts/my-schedule',
      queryParameters: {
        'PageNumber': pageNumber,
        'PageSize': 20,
        'FromDate': formatApiDate(fromDate),
        if (toDate != null) 'ToDate': formatApiDate(toDate),
      },
    );
    return AvailableShiftPage.fromJson(response.data as Map<String, dynamic>);
  }

  Future<AvailableShiftModel> getDetail(int shiftId) async {
    final response = await _apiClient.get('/api/staff/shifts/$shiftId');
    return AvailableShiftModel.fromJson(response.data as Map<String, dynamic>);
  }
}
