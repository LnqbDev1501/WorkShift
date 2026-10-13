import 'dart:developer';

import 'package:flutter/foundation.dart'; // nếu muốn chạy ngork thì comment dòng này
import 'package:dio/dio.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Central HTTP Client for connecting to ASP.NET Core Backend
class ApiClient {
  static final ApiClient _instance = ApiClient._internal();
  factory ApiClient() => _instance;

  late final Dio dio;
  static const String tokenKey = 'access_token';

  // comment đoạn này nếu muốn chạy web
  static String get defaultBaseUrl {
    if (kIsWeb) {
      return 'https://localhost:7193';
    }
    return 'http://10.0.2.2:5267';
  }

  // mở comment đoạn này nếu muốn chạy ngork
  // static String get defaultBaseUrl {
  //   // Dán link ngrok của bạn vào đây:
  //   return 'https://linguini-unselect-science.ngrok-free.dev'.trim();
  // }

  ApiClient._internal() {
    dio = Dio(
      BaseOptions(
        baseUrl: defaultBaseUrl.trim(),
        connectTimeout: const Duration(seconds: 15),
        receiveTimeout: const Duration(seconds: 15),
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
          'ngrok-skip-browser-warning': 'true',
        },
      ),
    );

    // Auto-attach Bearer Token to outgoing requests
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final prefs = await SharedPreferences.getInstance();
          final token = prefs.getString(tokenKey);

          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }

          log('🌐 [HTTP ${options.method}] ${options.uri}');
          return handler.next(options);
        },
        onResponse: (response, handler) {
          log('[HTTP ${response.statusCode}] ${response.requestOptions.path}');
          return handler.next(response);
        },
        onError: (DioException error, handler) {
          log(
            '[HTTP ${error.response?.statusCode}] ${error.requestOptions.path}',
          );
          return handler.next(error);
        },
      ),
    );
  }

  // Update base URL dynamically if needed
  void setBaseUrl(String newUrl) => dio.options.baseUrl = newUrl;

  // Token management
  Future<void> saveToken(String token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(tokenKey, token);
  }

  Future<void> clearToken() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(tokenKey);
  }

  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(tokenKey);
  }

  // HTTP Helper Methods
  Future<Response> get(
    String path, {
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      return await dio.get(path, queryParameters: queryParameters);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<Response> post(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      return await dio.post(path, data: data, queryParameters: queryParameters);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<Response> put(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      return await dio.put(path, data: data, queryParameters: queryParameters);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<Response> delete(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      return await dio.delete(
        path,
        data: data,
        queryParameters: queryParameters,
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  // Parse error messages from ASP.NET Core ProblemDetails or exceptions
  String _handleError(DioException error) {
    if (error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.receiveTimeout) {
      return 'Connection timed out. Please try again.';
    }

    if (error.type == DioExceptionType.connectionError) {
      return 'Cannot connect to server. Check your network or server URL.';
    }

    final response = error.response;
    if (response != null) {
      final data = response.data;

      // 1. Extract message from Map response (handles both Map<String, dynamic> and Map<dynamic, dynamic>)
      if (data is Map) {
        if (data['message'] != null &&
            data['message'].toString().trim().isNotEmpty) {
          return data['message'].toString().trim();
        }
        if (data['detail'] != null &&
            data['detail'].toString().trim().isNotEmpty) {
          return data['detail'].toString().trim();
        }
        if (data['error'] != null &&
            data['error'].toString().trim().isNotEmpty) {
          return data['error'].toString().trim();
        }
        if (data['errors'] != null && data['errors'] is Map) {
          final errMap = data['errors'] as Map;
          final firstList = errMap.values.isNotEmpty
              ? errMap.values.first
              : null;
          if (firstList is List && firstList.isNotEmpty) {
            return firstList.first.toString().trim();
          }
        }
        if (data['title'] != null &&
            data['title'].toString().trim().isNotEmpty) {
          return data['title'].toString().trim();
        }
      }

      // 2. Extract message if data is plain text
      if (data is String &&
          data.trim().isNotEmpty &&
          !data.trim().startsWith('<!DOCTYPE html')) {
        return data.trim();
      }

      switch (response.statusCode) {
        case 400:
          return 'Bad request. Please verify your data and try again.';
        case 401:
          return 'Session expired. Please log in again.';
        case 403:
          return 'You do not have permission to perform this action.';
        case 404:
          return 'Requested resource not found.';
        case 500:
          return 'Internal server error. Please try again later.';
      }
    }

    if (error.error != null) {
      final errStr = error.error.toString();
      if (errStr.contains('FormatException')) {
        return 'Invalid server response or URL format.';
      }
      return errStr;
    }

    return error.message ?? 'An unexpected error occurred.';
  }
}
