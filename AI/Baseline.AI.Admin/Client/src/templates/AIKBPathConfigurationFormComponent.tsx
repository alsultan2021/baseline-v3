import {
  type ActionCell,
  Button,
  ButtonType,
  CellType,
  ColumnContentType,
  Stack,
  type StringCell,
  Table,
  type TableAction,
  type TableCell,
  type TableColumn,
  type TableRow,
} from '@kentico/xperience-admin-components';
import { useFormComponentCommandProvider } from '@kentico/xperience-admin-base';
import React, { useEffect, useState } from 'react';
import Select, {
  type CSSObjectWithLabel,
  type GroupBase,
  type SingleValue,
  type StylesConfig,
} from 'react-select';
import {
  type AIKBChannel,
  type AIKBPathConfigProps,
  type AIKBPathConfiguration,
  type OptionType,
} from '../models';
import { AIKBIncludedPathEditor } from './AIKBIncludedPathEditor';

/** Main form component — renders a table of configured paths with inline editing. */
export const AIKBPathConfigurationFormComponent = (
  props: AIKBPathConfigProps,
): JSX.Element => {
  const [rows, setRows] = useState<TableRow[]>([]);
  const [showPathEdit, setShowPathEdit] = useState(false);
  const [showAddNew, setShowAddNew] = useState(true);
  const [editedPath, setEditedPath] = useState<AIKBPathConfiguration | null>(
    null,
  );
  const { executeCommand } = useFormComponentCommandProvider();

  // ── Table rows ──────────────────────────────────────────────────────────

  const prepareRows = (paths: AIKBPathConfiguration[]): TableRow[] => {
    if (!paths) return [];

    return paths.map((p) => {
      const channelCell: StringCell = {
        type: CellType.String,
        value: p.channelDisplayName || p.channelName,
      };

      const patternCell: StringCell = {
        type: CellType.String,
        value: p.includePattern,
      };

      const typesCell: StringCell = {
        type: CellType.String,
        value:
          p.contentTypes.length > 0
            ? p.contentTypes.map((t) => t.contentTypeDisplayName).join(', ')
            : '(all types)',
      };

      const priorityCell: StringCell = {
        type: CellType.String,
        value: String(p.priority),
      };

      const deleteAction: TableAction = {
        label: 'delete',
        icon: 'xp-bin',
        disabled: false,
        destructive: true,
      };

      const deletePath = async (): Promise<void> => {
        await executeCommand(props, 'DeletePath', p);
        const newPaths = props.value.filter(
          (x) =>
            !(
              x.channelName === p.channelName &&
              x.includePattern === p.includePattern
            ),
        );
        props.value = newPaths;
        if (props.onChange) props.onChange(newPaths);
        setRows(prepareRows(newPaths));
        setShowPathEdit(false);
        setShowAddNew(true);
      };

      const actionCell: ActionCell = {
        actions: [deleteAction],
        type: CellType.Action,
        onInvokeAction: deletePath,
      };

      const cells: TableCell[] = [
        channelCell,
        patternCell,
        typesCell,
        priorityCell,
        actionCell,
      ];

      return {
        identifier: `${p.channelName}::${p.includePattern}`,
        cells,
        disabled: false,
      } as TableRow;
    });
  };

  // ── Columns ─────────────────────────────────────────────────────────────

  const prepareColumns = (): TableColumn[] => [
    {
      name: 'Channel',
      visible: true,
      contentType: ColumnContentType.Text,
      caption: '',
      minWidth: 0,
      maxWidth: 300,
      sortable: true,
      searchable: true,
    },
    {
      name: 'Include Pattern',
      visible: true,
      contentType: ColumnContentType.Text,
      caption: '',
      minWidth: 0,
      maxWidth: 400,
      sortable: true,
      searchable: true,
    },
    {
      name: 'Content Types',
      visible: true,
      contentType: ColumnContentType.Text,
      caption: '',
      minWidth: 0,
      maxWidth: 500,
      sortable: false,
      searchable: false,
    },
    {
      name: 'Priority',
      visible: true,
      contentType: ColumnContentType.Text,
      caption: '',
      minWidth: 0,
      maxWidth: 100,
      sortable: true,
      searchable: false,
    },
    {
      name: 'Actions',
      visible: true,
      contentType: ColumnContentType.Action,
      caption: '',
      minWidth: 0,
      maxWidth: 100,
      sortable: false,
      searchable: false,
    },
  ];

  // ── Sync table on value change ──────────────────────────────────────────

  useEffect(() => {
    if (!props.value) props.value = [];
    if (props.onChange) props.onChange(props.value);
    setRows(prepareRows(props.value));
  }, [props?.value]);

  // ── Row click → open edit ───────────────────────────────────────────────

  const onRowClick = (identifier: unknown): void => {
    const id = identifier as string;
    const [channelName, includePattern] = id.split('::');
    const existing = props.value.find(
      (p) =>
        p.channelName === channelName && p.includePattern === includePattern,
    );
    if (existing) {
      setEditedPath({ ...existing });
      setShowPathEdit(true);
      setShowAddNew(false);
    }
  };

  // ── Add new path ────────────────────────────────────────────────────────

  const addNewPath = (): void => {
    setEditedPath({
      identifier: null,
      channelName: '',
      channelDisplayName: '',
      includePattern: '/%',
      excludePattern: null,
      contentTypes: [],
      priority: 0,
      includeChildren: true,
    });
    setShowPathEdit(true);
    setShowAddNew(false);
  };

  // ── Save from the editor sub-component ──────────────────────────────────

  const onSavePath = async (path: AIKBPathConfiguration): Promise<void> => {
    const isNew =
      !props.value.some(
        (p) =>
          p.channelName === path.channelName &&
          p.includePattern === path.includePattern,
      ) && path.identifier === null;

    if (isNew) {
      await executeCommand(props, 'AddPath', path);
      const newPaths = [...props.value, path];
      props.value = newPaths;
      if (props.onChange) props.onChange(newPaths);
      setRows(prepareRows(newPaths));
    } else {
      await executeCommand(props, 'SavePath', path);
      const newPaths = props.value.map((p) =>
        (p.identifier === path.identifier && path.identifier !== null) ||
        (p.channelName === path.channelName &&
          p.includePattern === path.includePattern)
          ? path
          : p,
      );
      props.value = newPaths;
      if (props.onChange) props.onChange(newPaths);
      setRows(prepareRows(newPaths));
    }

    setShowPathEdit(false);
    setShowAddNew(true);
    setEditedPath(null);
  };

  const onCancelEdit = (): void => {
    setShowPathEdit(false);
    setShowAddNew(true);
    setEditedPath(null);
  };

  // ── Channel select styles (consistent with Lucene's look) ──────────────

  /* eslint-disable @typescript-eslint/naming-convention */
  const channelSelectStyle: StylesConfig<
    OptionType,
    false,
    GroupBase<OptionType>
  > = {
    control: (styles, { isFocused }) =>
      ({
        ...styles,
        backgroundColor: 'white',
        borderColor: isFocused ? 'black' : 'gray',
        '&:hover': { borderColor: 'black' },
        borderRadius: 20,
        boxShadow: 'gray',
        padding: 2,
        minHeight: 'fit-content',
      }) as CSSObjectWithLabel,
    option: (styles, { isSelected }) => ({
      ...styles,
      backgroundColor: isSelected ? '#bab4f0' : 'white',
      '&:hover': { backgroundColor: isSelected ? '#a097f7' : 'lightgray' },
      color: isSelected ? 'purple' : 'black',
      cursor: 'pointer',
    }),
    input: (styles) => ({ ...styles }),
    container: (styles) =>
      ({ ...styles, borderColor: 'gray' }) as CSSObjectWithLabel,
    placeholder: (styles) => ({ ...styles }),
    dropdownIndicator: (styles, state): CSSObjectWithLabel =>
      ({
        ...styles,
        transform: state.selectProps.menuIsOpen
          ? 'rotate(180deg)'
          : 'rotate(0deg)',
      }) as CSSObjectWithLabel,
    menu: (styles) => ({ ...styles, zIndex: 9999 }),
  };
  /* eslint-enable @typescript-eslint/naming-convention */

  return (
    <Stack>
      <Table columns={prepareColumns()} rows={rows} onRowClick={onRowClick} />

      {showPathEdit && editedPath && (
        <div
          style={{
            marginTop: '20px',
            padding: '16px',
            border: '1px solid #ddd',
            borderRadius: 8,
          }}
        >
          <AIKBIncludedPathEditor
            path={editedPath}
            possibleChannels={props.possibleChannels}
            possibleContentTypeItems={props.possibleContentTypeItems}
            channelSelectStyle={channelSelectStyle}
            onSave={onSavePath}
            onCancel={onCancelEdit}
          />
        </div>
      )}

      {showAddNew && (
        <div style={{ marginTop: '20px' }}>
          <Button
            type={ButtonType.Button}
            label="Add new path"
            onClick={addNewPath}
          />
        </div>
      )}
    </Stack>
  );
};
