import '../../models/shifts/shift_swap_model.dart';
import '../core/api_client.dart';
import '../auth/auth_service.dart';

class ShiftSwapService {
  final ApiClient _api;
  final AuthService _auth;
  static const _path = '/api/staff/shift-swap-requests';
  ShiftSwapService({ApiClient? apiClient, AuthService? authService})
    : _api = apiClient ?? ApiClient(),
      _auth = authService ?? AuthService();

  Future<String> getCurrentUserId() async {
    final user = await _auth.getSavedUser();
    final id = user?['userId'] as String?;
    if (id == null || id.isEmpty) throw 'Session expired. Please log in again.';
    return id;
  }

  Future<ShiftSwapPage> getRequests({
    int pageNumber = 1,
    String direction = 'ALL',
    String? status,
  }) async {
    final response = await _api.get(
      _path,
      queryParameters: {
        'PageNumber': pageNumber,
        'PageSize': 20,
        'Direction': direction,
        'Status': ?status,
      },
    );
    return ShiftSwapPage.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ShiftSwapModel> getDetail(int id) async => ShiftSwapModel.fromJson(
    (await _api.get('$_path/$id')).data as Map<String, dynamic>,
  );

  Future<List<ShiftSwapCandidateModel>> getCandidates(
    int sourceAssignmentId,
  ) async {
    final response = await _api.get(
      '$_path/candidates',
      queryParameters: {'sourceAssignmentId': sourceAssignmentId},
    );
    return List.unmodifiable(
      (response.data as List).map(
        (item) =>
            ShiftSwapCandidateModel.fromJson(item as Map<String, dynamic>),
      ),
    );
  }

  Future<ShiftSwapModel> submit({
    required int sourceAssignmentId,
    required int targetAssignmentId,
    String? reason,
  }) async {
    final response = await _api.post(
      _path,
      data: {
        'sourceAssignmentId': sourceAssignmentId,
        'targetAssignmentId': targetAssignmentId,
        'reason': reason,
      },
    );
    return ShiftSwapModel.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ShiftSwapModel> accept(int id) => _respond(id, 'accept');
  Future<ShiftSwapModel> cancel(int id, String? note) =>
      _respond(id, 'cancel', note);
  Future<ShiftSwapModel> reject(int id, String? note) =>
      _respond(id, 'reject', note);
  Future<ShiftSwapModel> _respond(int id, String action, [String? note]) async {
    final response = await _api.post(
      '$_path/$id/$action',
      data: action == 'accept' ? null : {'note': note},
    );
    return ShiftSwapModel.fromJson(response.data as Map<String, dynamic>);
  }
}
