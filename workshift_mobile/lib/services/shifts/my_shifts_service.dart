import '../../models/shifts/available_shift_model.dart';
import '../core/api_client.dart';

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
        'FromDate': _calendarDate(fromDate),
        if (toDate != null) 'ToDate': _calendarDate(toDate),
      },
    );
    return AvailableShiftPage.fromJson(response.data as Map<String, dynamic>);
  }

  Future<AvailableShiftModel> getDetail(int shiftId) async {
    final response = await _apiClient.get('/api/staff/shifts/$shiftId');
    return AvailableShiftModel.fromJson(response.data as Map<String, dynamic>);
  }

  String _calendarDate(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
}
