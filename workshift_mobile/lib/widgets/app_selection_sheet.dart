import 'package:flutter/material.dart';

class AppSelectionOption<T> {
  final T value;
  final String title;
  final String subtitle;
  const AppSelectionOption({
    required this.value,
    required this.title,
    required this.subtitle,
  });
}

Future<T?> showAppSelectionSheet<T>(
  BuildContext context, {
  required String title,
  required List<AppSelectionOption<T>> options,
}) => showModalBottomSheet<T>(
  context: context,
  isScrollControlled: true,
  useSafeArea: true,
  showDragHandle: true,
  builder: (_) => _SelectionSheet<T>(title: title, options: options),
);

class _SelectionSheet<T> extends StatefulWidget {
  final String title;
  final List<AppSelectionOption<T>> options;
  const _SelectionSheet({required this.title, required this.options});
  @override
  State<_SelectionSheet<T>> createState() => _SelectionSheetState<T>();
}

class _SelectionSheetState<T> extends State<_SelectionSheet<T>> {
  String _query = '';
  @override
  Widget build(BuildContext context) {
    final options = widget.options
        .where((o) => '${o.title} ${o.subtitle}'.toLowerCase().contains(_query))
        .toList();
    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
      child: SizedBox(
        height: MediaQuery.sizeOf(context).height * 0.65,
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    widget.title,
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    decoration: const InputDecoration(
                      hintText: 'Search name or date',
                      prefixIcon: Icon(Icons.search_rounded),
                    ),
                    onChanged: (value) =>
                        setState(() => _query = value.trim().toLowerCase()),
                  ),
                ],
              ),
            ),
            Expanded(
              child: options.isEmpty
                  ? const Center(child: Text('No matches found.'))
                  : ListView.separated(
                      itemCount: options.length,
                      separatorBuilder: (_, _) => const Divider(height: 1),
                      itemBuilder: (context, index) => ListTile(
                        title: Text(options[index].title),
                        subtitle: Text(options[index].subtitle),
                        trailing: const Icon(Icons.chevron_right_rounded),
                        onTap: () =>
                            Navigator.pop(context, options[index].value),
                      ),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}
