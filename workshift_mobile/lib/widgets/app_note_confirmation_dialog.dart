import 'package:flutter/material.dart';
import 'app_confirmation_dialog.dart';

Future<String?> showAppNoteConfirmation(
  BuildContext context, {
  required String title,
  required String message,
  required String confirmLabel,
  bool isDestructive = false,
  bool showNote = true,
  String initialNote = '',
}) => showDialog<String>(
  context: context,
  builder: (_) => _NoteConfirmation(
    title: title,
    message: message,
    confirmLabel: confirmLabel,
    isDestructive: isDestructive,
    showNote: showNote,
    initialNote: initialNote,
  ),
);

class _NoteConfirmation extends StatefulWidget {
  final String title, message, confirmLabel, initialNote;
  final bool isDestructive, showNote;
  const _NoteConfirmation({
    required this.title,
    required this.message,
    required this.confirmLabel,
    required this.initialNote,
    required this.isDestructive,
    required this.showNote,
  });
  @override
  State<_NoteConfirmation> createState() => _NoteConfirmationState();
}

class _NoteConfirmationState extends State<_NoteConfirmation> {
  late final _note = TextEditingController(text: widget.initialNote);
  final _form = GlobalKey<FormState>();
  @override
  void dispose() {
    _note.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AppConfirmationDialog(
    title: widget.title,
    confirmLabel: widget.confirmLabel,
    isDestructive: widget.isDestructive,
    onCancel: () => Navigator.pop(context),
    onConfirm: () {
      if (_form.currentState!.validate()) {
        Navigator.pop(context, _note.text.trim());
      }
    },
    content: Form(
      key: _form,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(widget.message, style: const TextStyle(height: 1.5)),
          if (widget.showNote) ...[
            const SizedBox(height: 16),
            TextFormField(
              controller: _note,
              minLines: 2,
              maxLines: 4,
              maxLength: 500,
              decoration: const InputDecoration(labelText: 'Note (optional)'),
              validator: (value) =>
                  (value?.length ?? 0) > 500 ? 'Maximum 500 characters.' : null,
            ),
          ],
        ],
      ),
    ),
  );
}
