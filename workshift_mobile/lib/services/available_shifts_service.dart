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
        if (fromDate != null) 'FromDate': _calendarDate(fromDate),
        if (toDate != null) 'ToDate': _calendarDate(toDate),
      },
    );
    return AvailableShiftPage.fromJson(response.data as Map<String, dynamic>);
  }

  // These are Vietnam calendar dates, not instants to convert to UTC.
  String _calendarDate(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-'
      '${date.month.toString().padLeft(2, '0')}-'
      '${date.day.toString().padLeft(2, '0')}';
}
