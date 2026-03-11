import {
  Button,
  ButtonType,
  Input,
  Stack,
} from '@kentico/xperience-admin-components';
import React, { type CSSProperties, useState } from 'react';
import { IoCheckmarkSharp } from 'react-icons/io5';
import { MdOutlineCancel } from 'react-icons/md';
import { RxCross1 } from 'react-icons/rx';
import Select, {
  type CSSObjectWithLabel,
  type ClearIndicatorProps,
  type GroupBase,
  type MultiValue,
  type MultiValueRemoveProps,
  type OptionProps,
  type SingleValue,
  type StylesConfig,
  components,
} from 'react-select';
import { Tooltip } from 'react-tooltip';
import {
  type AIKBChannel,
  type AIKBContentType,
  type AIKBPathConfiguration,
  type OptionType,
} from '../models';

interface AIKBIncludedPathEditorProps {
  path: AIKBPathConfiguration;
  possibleChannels: AIKBChannel[] | null;
  possibleContentTypeItems: AIKBContentType[] | null;
  channelSelectStyle: StylesConfig<OptionType, false, GroupBase<OptionType>>;
  onSave: (path: AIKBPathConfiguration) => Promise<void>;
  onCancel: () => void;
}

/** Inline editor for a single KB path — channel, patterns, content types, priority, children. */
export const AIKBIncludedPathEditor = ({
  path: initialPath,
  possibleChannels,
  possibleContentTypeItems,
  channelSelectStyle,
  onSave,
  onCancel,
}: AIKBIncludedPathEditorProps): JSX.Element => {
  const [channelName, setChannelName] = useState(initialPath.channelName);
  const [channelDisplayName, setChannelDisplayName] = useState(
    initialPath.channelDisplayName,
  );
  const [includePattern, setIncludePattern] = useState(
    initialPath.includePattern,
  );
  const [excludePattern, setExcludePattern] = useState(
    initialPath.excludePattern ?? '',
  );
  const [priority, setPriority] = useState(String(initialPath.priority));
  const [includeChildren, setIncludeChildren] = useState(
    initialPath.includeChildren,
  );
  const [isClearIndicatorHover, setIsClearIndicatorHover] = useState(false);

  const [contentTypesValue, setContentTypesValue] = useState<OptionType[]>(
    initialPath.contentTypes.map((t) => ({
      value: t.contentTypeName,
      label: t.contentTypeDisplayName,
    })),
  );

  // ── Channel options ─────────────────────────────────────────────────────

  const channelOptions: OptionType[] =
    possibleChannels?.map((c) => ({
      value: c.channelName,
      label: c.channelDisplayName,
    })) ?? [];

  const selectedChannelOption: OptionType | null = channelName
    ? (channelOptions.find((o) => o.value === channelName) ?? null)
    : null;

  const selectChannel = (newValue: SingleValue<OptionType>): void => {
    if (newValue) {
      setChannelName(newValue.value);
      setChannelDisplayName(newValue.label);
    }
  };

  // ── Content type options ────────────────────────────────────────────────

  const contentTypeOptions: OptionType[] =
    possibleContentTypeItems?.map((t) => ({
      value: t.contentTypeName,
      label: t.contentTypeDisplayName,
    })) ?? [];

  const selectContentTypes = (newValue: MultiValue<OptionType>): void => {
    setContentTypesValue(newValue as OptionType[]);
  };

  // ── Content type multi-select styles ────────────────────────────────────

  /* eslint-disable @typescript-eslint/naming-convention */
  const multiSelectStyle: StylesConfig<
    OptionType,
    true,
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
    multiValue: (styles) =>
      ({
        ...styles,
        backgroundColor: '#287ab5',
        borderRadius: 10,
        height: 35,
        alignItems: 'center',
      }) as CSSObjectWithLabel,
    multiValueLabel: (styles) =>
      ({
        ...styles,
        color: 'white',
        fontSize: 14,
        alignContent: 'center',
      }) as CSSObjectWithLabel,
    indicatorSeparator: () => ({}),
    dropdownIndicator: (styles, state): CSSObjectWithLabel =>
      ({
        ...styles,
        transform: state.selectProps.menuIsOpen
          ? 'rotate(180deg)'
          : 'rotate(0deg)',
      }) as CSSObjectWithLabel,
    multiValueRemove: (styles) =>
      ({
        ...styles,
        '&:hover': {
          background: '#287ab5',
          borderRadius: 10,
          cursor: 'pointer',
          filter: 'grayscale(40%)',
          height: '100%',
        },
      }) as CSSObjectWithLabel,
    menu: (styles) => ({ ...styles, zIndex: 9999 }),
  };
  /* eslint-enable @typescript-eslint/naming-convention */

  // ── Custom multi-select components ──────────────────────────────────────

  const MultiValueRemoveStyle: CSSProperties = {
    color: 'white',
    height: '20',
    width: '30',
  };

  const MultiValueRemove = (
    removeProps: MultiValueRemoveProps<OptionType>,
  ): JSX.Element => (
    <components.MultiValueRemove {...removeProps}>
      <RxCross1 style={MultiValueRemoveStyle} />
    </components.MultiValueRemove>
  );

  const Option = (
    optionProps: OptionProps<OptionType, true, GroupBase<OptionType>>,
  ): JSX.Element => (
    <components.Option {...optionProps}>
      {optionProps.isSelected ? (
        <IoCheckmarkSharp style={{ width: 30, alignContent: 'center' }} />
      ) : (
        <span style={{ width: 30, display: 'inline-block' }}></span>
      )}
      {optionProps.children}
    </components.Option>
  );

  const IndicatorStyle: CSSProperties = {
    color: 'black',
    width: '80%',
    height: '80%',
  };

  const ClearIndicator = (
    clearProps: ClearIndicatorProps<OptionType>,
  ): JSX.Element => (
    <components.ClearIndicator {...clearProps}>
      <Tooltip id="clear-ct-tooltip" />
      <span
        style={{
          background: isClearIndicatorHover ? 'lightgray' : 'white',
          width: 25,
          height: 25,
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          borderRadius: 5,
          cursor: isClearIndicatorHover ? 'pointer' : 'default',
        }}
      >
        <MdOutlineCancel
          style={IndicatorStyle}
          onMouseEnter={() => setIsClearIndicatorHover(true)}
          onMouseLeave={() => setIsClearIndicatorHover(false)}
          data-tooltip-id="clear-ct-tooltip"
          data-tooltip-content="Clear selection"
        />
      </span>
    </components.ClearIndicator>
  );

  // ── Save handler ────────────────────────────────────────────────────────

  const handleSave = async (): Promise<void> => {
    if (!channelName) {
      alert('Please select a website channel.');
      return;
    }
    if (!includePattern) {
      alert('Include pattern is required.');
      return;
    }

    await onSave({
      identifier: initialPath.identifier,
      channelName,
      channelDisplayName,
      includePattern,
      excludePattern: excludePattern || null,
      contentTypes: contentTypesValue.map((o) => ({
        contentTypeName: o.value,
        contentTypeDisplayName: o.label,
      })),
      priority: parseInt(priority, 10) || 0,
      includeChildren,
    });
  };

  // ── Render ──────────────────────────────────────────────────────────────

  return (
    <Stack>
      {/* Channel selector */}
      <div>
        <div className="label-wrapper___AcszK">
          <label className="label___WET63">Website Channel</label>
        </div>
        <Select
          closeMenuOnSelect={true}
          isMulti={false}
          placeholder="Select website channel"
          value={selectedChannelOption}
          options={channelOptions}
          onChange={selectChannel}
          styles={channelSelectStyle}
        />
      </div>

      {/* Include pattern */}
      <div style={{ marginTop: 12 }}>
        <Input
          label="Include Pattern"
          value={includePattern}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setIncludePattern(e.target.value)
          }
        />
      </div>

      {/* Exclude pattern */}
      <div style={{ marginTop: 12 }}>
        <Input
          label="Exclude Pattern"
          value={excludePattern}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setExcludePattern(e.target.value)
          }
        />
      </div>

      {/* Content types multi-select */}
      <div style={{ marginTop: 12 }}>
        <div className="label-wrapper___AcszK">
          <label className="label___WET63">Content Types</label>
        </div>
        <Select
          isMulti
          closeMenuOnSelect={false}
          defaultValue={contentTypesValue}
          placeholder="Select content types (leave empty for all)"
          options={contentTypeOptions}
          onChange={selectContentTypes}
          styles={multiSelectStyle}
          hideSelectedOptions={false}
          components={{ MultiValueRemove, ClearIndicator, Option }}
          theme={(theme) => ({
            ...theme,
            height: 40,
            borderRadius: 0,
            borderColor: 'gray',
          })}
        />
      </div>

      {/* Priority */}
      <div style={{ marginTop: 12 }}>
        <Input
          label="Priority"
          value={priority}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setPriority(e.target.value)
          }
        />
      </div>

      {/* Include children */}
      <div
        style={{ marginTop: 12, display: 'flex', alignItems: 'center', gap: 8 }}
      >
        <input
          type="checkbox"
          id="includeChildren"
          checked={includeChildren}
          onChange={(e) => setIncludeChildren(e.target.checked)}
          style={{ width: 18, height: 18, cursor: 'pointer' }}
        />
        <label htmlFor="includeChildren" style={{ cursor: 'pointer' }}>
          Include Children
        </label>
      </div>

      {/* Action buttons */}
      <div style={{ marginTop: 16, display: 'flex', gap: 12 }}>
        <Button
          type={ButtonType.Button}
          label="Save Path"
          onClick={handleSave}
        />
        <Button type={ButtonType.Button} label="Cancel" onClick={onCancel} />
      </div>
    </Stack>
  );
};
