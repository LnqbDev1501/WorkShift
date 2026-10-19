import 'file_download_stub.dart'
    if (dart.library.html) 'file_download_web.dart';

void downloadPdfBytes(List<int> bytes, String fileName) {
  downloadFileFromBytes(bytes, fileName);
}

void previewPdfInBrowser(List<int> bytes) {
  openPdfInNewTab(bytes);
}
