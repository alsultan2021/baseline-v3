import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import {
  FormComponentProps,
  useFormComponentCommandProvider,
} from '@kentico/xperience-admin-base';
import {
  AssetTilePreview,
  BrowseButton,
  Button,
  ButtonColor,
  ButtonSize,
  Callout,
  CalloutPlacementType,
  CalloutType,
  Dialog,
  Divider,
  DividerOrientation,
  FileDropOverlay,
  FormItemWrapper,
  Headline,
  HeadlineSize,
  Inline,
  LayoutAlignment,
  MenuItem,
  Paper,
  PaperElevation,
  ProgressBar,
  Select,
  Spacing,
  Spinner,
  Stack,
  Switch,
  SwitchSize,
  TextArea,
  TextWithLabel,
} from '@kentico/xperience-admin-components';

/* ─────────────────────────── Types ─────────────────────────── */

interface AssetBaseClientItem {
  identifier: string;
  name: string;
  extension: string;
  size: number;
  isImage: boolean;
  url: string;
  width: number;
  height: number;
  lastModified: string;
}

interface AssetClientItem {
  original: AssetBaseClientItem;
}

interface ImageEditorFormComponentProps extends FormComponentProps {
  assetMetadata?: AssetClientItem;
  allowedExtensions?: string[];
  chunkSize?: number;
  aiGenerationAvailable?: boolean;
  label?: string;
  explanationText?: string;
  tooltip?: string;
  invalid?: boolean;
  validationMessage?: string;
  disabled?: boolean;
  required?: boolean;
}

/* ── Command arg/result types ── */

interface UploadChunkArgs {
  chunkId: number;
  fileName: string;
  fileSize: number;
  fileIdentifier?: string;
}
interface UploadChunkResult {
  identifier?: string;
  name?: string;
  size?: number;
  errorMessage?: string;
}

interface CompleteUploadArgs {
  fileName: string;
  fileIdentifier: string;
  fileSize: number;
}
interface CompleteUploadResult {
  assetMetadata?: AssetClientItem;
}

interface RemoveUploadedFileArgs {
  assetMetadata?: AssetClientItem;
}

interface ApplyEditsArgs {
  fileIdentifier: string;
  fileName: string;
  parameters: EditParameters;
}
interface ApplyEditsResult {
  assetMetadata?: AssetClientItem;
  errorMessage?: string;
}

interface GenerateImageArgs {
  prompt: string;
  negativePrompt: string;
  aspectRatio: string;
  referenceImageBase64?: string;
  referenceImageMimeType?: string;
}
interface GenerateImageResult {
  assetMetadata?: AssetClientItem;
  errorMessage?: string;
}

interface EditParameters {
  crop?: { x: number; y: number; width: number; height: number } | null;
  rotationDegrees: number;
  flipHorizontal: boolean;
  flipVertical: boolean;
  brightness: number;
  contrast: number;
  saturation: number;
  filterPreset: string;
  resize?: { width: number; height: number } | null;
  outputFormat: string;
  quality: number;
}

/* ── Crop selection state ── */

interface CropSelection {
  x: number;
  y: number;
  width: number;
  height: number;
}

/* ── Constants ── */

const defaultChunkSize = 4 * 1024 * 1024;
const defaultAllowedExtensions = [
  'png',
  'jpg',
  'jpeg',
  'gif',
  'webp',
  'bmp',
  'svg',
];
const filterPresets = ['none', 'grayscale', 'sepia', 'warm', 'cool'] as const;
const formatOptions = ['png', 'jpeg', 'webp'] as const;
const aspectRatioOptions = [
  { value: '1:1', label: '1:1 Square' },
  { value: '4:3', label: '4:3 Landscape' },
  { value: '3:4', label: '3:4 Portrait' },
  { value: '16:9', label: '16:9 Wide' },
  { value: '9:16', label: '9:16 Tall' },
] as const;

type UploadPhase = 'idle' | 'uploading' | 'error';

interface UploadState {
  phase: UploadPhase;
  progress: number;
  errorMessage?: string;
}

/* ── Helpers ── */

function buildAcceptString(extensions: string[]): string {
  return extensions.map((e) => `.${e}`).join(',');
}

function cssFilterString(
  brightness: number,
  contrast: number,
  saturation: number,
  filterPreset: string,
): string {
  const parts: string[] = [];
  if (brightness !== 1) parts.push(`brightness(${brightness})`);
  if (contrast !== 1) parts.push(`contrast(${contrast})`);
  if (saturation !== 1) parts.push(`saturate(${saturation})`);
  if (filterPreset === 'grayscale') parts.push('grayscale(1)');
  if (filterPreset === 'sepia') parts.push('sepia(1)');
  return parts.join(' ') || 'none';
}

/* ═══════════════════════════════════════════════════════════════
   Image Editor Form Component
   ═══════════════════════════════════════════════════════════════ */

export const ImageEditorFormComponent: React.FC<
  ImageEditorFormComponentProps
> = (props) => {
  const {
    assetMetadata: initialAsset,
    allowedExtensions = defaultAllowedExtensions,
    chunkSize = defaultChunkSize,
    aiGenerationAvailable = false,
    label,
    explanationText,
    tooltip,
    invalid,
    validationMessage,
    disabled,
    required,
    onChange,
  } = props;

  const { executeCommand } = useFormComponentCommandProvider();
  const abortRef = useRef<AbortController | null>(null);

  /* ─── State ─── */
  const [asset, setAsset] = useState<AssetClientItem | undefined>(initialAsset);
  const [uploadState, setUploadState] = useState<UploadState>({
    phase: 'idle',
    progress: 0,
  });
  const [localPreviewUrl, setLocalPreviewUrl] = useState<string | undefined>();

  // Editor state
  const [editorOpen, setEditorOpen] = useState(false);
  const [applying, setApplying] = useState(false);

  // Edit parameters
  const [rotation, setRotation] = useState(0);
  const [flipH, setFlipH] = useState(false);
  const [flipV, setFlipV] = useState(false);
  const [brightness, setBrightness] = useState(1.0);
  const [contrast, setContrast] = useState(1.0);
  const [saturation, setSaturation] = useState(1.0);
  const [filterPreset, setFilterPreset] = useState<string>('none');
  const [outputFormat, setOutputFormat] = useState<string>('png');
  const [quality, setQuality] = useState(90);

  // Crop state
  const [cropEnabled, setCropEnabled] = useState(false);
  const [cropSelection, setCropSelection] = useState<CropSelection | null>(
    null,
  );
  const cropRef = useRef<HTMLDivElement>(null);
  const cropStartRef = useRef<{ startX: number; startY: number } | null>(null);

  // Adjust image dialog state (Kentico-style: format + quality only)
  const [adjustDialogOpen, setAdjustDialogOpen] = useState(false);
  const [adjustFormat, setAdjustFormat] = useState<string>('png');
  const [adjustQuality, setAdjustQuality] = useState(90);
  const [adjustApplying, setAdjustApplying] = useState(false);
  const [adjustPreviewUrl, setAdjustPreviewUrl] = useState<
    string | undefined
  >();

  // AI generation state
  const [showAiPanel, setShowAiPanel] = useState(false);
  const [aiPrompt, setAiPrompt] = useState('');
  const [aiNegativePrompt, setAiNegativePrompt] = useState('');
  const [aiAspectRatio, setAiAspectRatio] = useState('1:1');
  const [generating, setGenerating] = useState(false);
  const [aiError, setAiError] = useState<string | undefined>();

  // Reference image state (Freepik-style: optional image to guide generation)
  const [referenceImageFile, setReferenceImageFile] = useState<File | null>(
    null,
  );
  const [referenceImagePreview, setReferenceImagePreview] = useState<
    string | undefined
  >();
  const refImageInputRef = useRef<HTMLInputElement>(null);

  const isReadOnly = disabled === true;
  const isUploading = uploadState.phase === 'uploading';
  const imageSrc = localPreviewUrl ?? (asset?.original.url || undefined);

  /* ─── Upload ─── */

  const uploadFiles = useCallback(
    async (files: FileList) => {
      const file = files[0];
      if (!file) return;

      const abort = new AbortController();
      abortRef.current = abort;
      setUploadState({ phase: 'uploading', progress: 0 });

      try {
        const totalChunks = Math.ceil(file.size / chunkSize);
        let fileIdentifier: string | undefined;

        for (let chunkId = 0; chunkId < totalChunks; chunkId++) {
          if (abort.signal.aborted) return;
          const start = chunkId * chunkSize;
          const end = Math.min(start + chunkSize, file.size);
          const blob = file.slice(start, end);
          const dt = new DataTransfer();
          dt.items.add(new File([blob], file.name));

          const res = await executeCommand<UploadChunkResult, UploadChunkArgs>(
            props,
            'UploadChunk',
            {
              chunkId,
              fileName: file.name,
              fileSize: file.size,
              fileIdentifier,
            },
            dt.files,
            abort,
          );

          if (res?.errorMessage) {
            setUploadState({
              phase: 'error',
              progress: 0,
              errorMessage: res.errorMessage,
            });
            return;
          }

          if (!fileIdentifier && res?.identifier) {
            fileIdentifier = res.identifier;
          }

          setUploadState((p) => ({
            ...p,
            progress: Math.round(((chunkId + 1) / totalChunks) * 100),
          }));
        }

        if (!fileIdentifier) {
          setUploadState({
            phase: 'error',
            progress: 0,
            errorMessage: 'Upload failed: no identifier.',
          });
          return;
        }

        const complete = await executeCommand<
          CompleteUploadResult,
          CompleteUploadArgs
        >(
          props,
          'CompleteUpload',
          { fileName: file.name, fileIdentifier, fileSize: file.size },
          undefined,
          abort,
        );

        if (complete?.assetMetadata) {
          setAsset(complete.assetMetadata);
          setLocalPreviewUrl(URL.createObjectURL(file));
          onChange?.(complete.assetMetadata.original as never);
        }

        setUploadState({ phase: 'idle', progress: 0 });
      } catch {
        if (!abort.signal.aborted) {
          setUploadState({
            phase: 'error',
            progress: 0,
            errorMessage: 'Upload failed unexpectedly.',
          });
        }
      }
    },
    [chunkSize, executeCommand, onChange, props],
  );

  const handleRemove = useCallback(async () => {
    await executeCommand<void, RemoveUploadedFileArgs>(
      props,
      'RemoveUploadedFile',
      { assetMetadata: asset },
    );
    setAsset(undefined);
    setLocalPreviewUrl(undefined);
    onChange?.(null as never);
  }, [asset, executeCommand, onChange, props]);

  const handleCancelUpload = useCallback(() => {
    abortRef.current?.abort();
    setUploadState({ phase: 'idle', progress: 0 });
  }, []);

  /* ─── AI Image Generation ─── */

  const handleGenerateImage = useCallback(async () => {
    if (!aiPrompt.trim()) return;
    setGenerating(true);
    setAiError(undefined);

    try {
      // Read reference image as base64 if provided
      let refBase64: string | undefined;
      let refMime: string | undefined;
      if (referenceImageFile) {
        refBase64 = await new Promise<string>((resolve, reject) => {
          const reader = new FileReader();
          reader.onload = () => {
            const dataUrl = reader.result as string;
            // Strip the data:...;base64, prefix
            resolve(dataUrl.split(',')[1]);
          };
          reader.onerror = reject;
          reader.readAsDataURL(referenceImageFile);
        });
        refMime = referenceImageFile.type || 'image/png';
      }

      const result = await executeCommand<
        GenerateImageResult,
        GenerateImageArgs
      >(props, 'GenerateImage', {
        prompt: aiPrompt.trim(),
        negativePrompt: aiNegativePrompt.trim(),
        aspectRatio: aiAspectRatio,
        referenceImageBase64: refBase64,
        referenceImageMimeType: refMime,
      });

      if (result?.errorMessage) {
        setAiError(result.errorMessage);
        return;
      }

      if (result?.assetMetadata) {
        setAsset(result.assetMetadata);
        setLocalPreviewUrl(
          `${result.assetMetadata.original.url}?t=${Date.now()}`,
        );
        onChange?.(result.assetMetadata.original as never);
        setShowAiPanel(false);
        setAiPrompt('');
        setAiNegativePrompt('');
      }
    } catch (err) {
      setAiError(
        err instanceof Error ? err.message : 'Generation failed unexpectedly.',
      );
    } finally {
      setGenerating(false);
    }
  }, [
    aiPrompt,
    aiNegativePrompt,
    aiAspectRatio,
    referenceImageFile,
    executeCommand,
    props,
    onChange,
  ]);

  /* ─── Editor controls ─── */

  /** Open AI panel; auto-populate reference image from current asset */
  const openAiPanel = useCallback(() => {
    setShowAiPanel(true);
    setAiError(undefined);
    if (asset && imageSrc && !referenceImageFile) {
      fetch(imageSrc)
        .then((res) => res.blob())
        .then((blob) => {
          const file = new File([blob], asset.original.name, {
            type: blob.type || 'image/png',
          });
          setReferenceImageFile(file);
          setReferenceImagePreview(URL.createObjectURL(file));
        })
        .catch(() => {
          /* silently ignore — user can still upload manually */
        });
    }
  }, [asset, imageSrc, referenceImageFile]);

  const resetEditorState = useCallback(() => {
    setRotation(0);
    setFlipH(false);
    setFlipV(false);
    setBrightness(1.0);
    setContrast(1.0);
    setSaturation(1.0);
    setFilterPreset('none');
    setOutputFormat('png');
    setQuality(90);
    setCropEnabled(false);
    setCropSelection(null);
  }, []);

  const openEditor = useCallback(() => {
    resetEditorState();
    // Use current extension as default output format
    if (asset?.original.extension) {
      const ext = asset.original.extension.replace('.', '').toLowerCase();
      if (['png', 'jpeg', 'jpg', 'webp'].includes(ext)) {
        setOutputFormat(ext === 'jpg' ? 'jpeg' : ext);
      }
    }
    setEditorOpen(true);
  }, [asset, resetEditorState]);

  /* ─── Adjust image dialog (Kentico-style) ─── */

  const openAdjustDialog = useCallback(() => {
    if (!asset) return;
    const ext =
      asset.original.extension?.replace('.', '').toLowerCase() || 'png';
    const fmt = ['png', 'jpeg', 'jpg', 'webp'].includes(ext)
      ? ext === 'jpg'
        ? 'jpeg'
        : ext
      : 'png';
    setAdjustFormat(fmt);
    setAdjustQuality(90);
    setAdjustPreviewUrl(undefined);
    setAdjustDialogOpen(true);
  }, [asset]);

  const closeAdjustDialog = useCallback(() => {
    setAdjustDialogOpen(false);
    setAdjustPreviewUrl(undefined);
  }, []);

  const handleApplyAdjust = useCallback(async () => {
    if (!asset) return;
    setAdjustApplying(true);
    try {
      const params: EditParameters = {
        crop: null,
        rotationDegrees: 0,
        flipHorizontal: false,
        flipVertical: false,
        brightness: 1,
        contrast: 1,
        saturation: 1,
        filterPreset: 'none',
        resize: null,
        outputFormat: adjustFormat,
        quality: adjustQuality,
      };
      const result = await executeCommand<ApplyEditsResult, ApplyEditsArgs>(
        props,
        'ApplyEdits',
        {
          fileIdentifier: asset.original.identifier,
          fileName: asset.original.name,
          parameters: params,
        },
      );
      if (result?.errorMessage) {
        alert(`Adjust failed: ${result.errorMessage}`);
        return;
      }
      if (result?.assetMetadata) {
        setAsset(result.assetMetadata);
        setLocalPreviewUrl(
          `${result.assetMetadata.original.url}?t=${Date.now()}`,
        );
        onChange?.(result.assetMetadata.original as never);
      }
      setAdjustDialogOpen(false);
      setAdjustPreviewUrl(undefined);
    } finally {
      setAdjustApplying(false);
    }
  }, [asset, adjustFormat, adjustQuality, executeCommand, props, onChange]);

  const closeEditor = useCallback(() => {
    setEditorOpen(false);
    resetEditorState();
  }, [resetEditorState]);

  const handleApplyEdits = useCallback(async () => {
    if (!asset) return;
    setApplying(true);

    try {
      const params: EditParameters = {
        crop: cropSelection
          ? {
              x: Math.round(cropSelection.x),
              y: Math.round(cropSelection.y),
              width: Math.round(cropSelection.width),
              height: Math.round(cropSelection.height),
            }
          : null,
        rotationDegrees: rotation,
        flipHorizontal: flipH,
        flipVertical: flipV,
        brightness,
        contrast,
        saturation,
        filterPreset,
        resize: null,
        outputFormat,
        quality,
      };

      const result = await executeCommand<ApplyEditsResult, ApplyEditsArgs>(
        props,
        'ApplyEdits',
        {
          fileIdentifier: asset.original.identifier,
          fileName: asset.original.name,
          parameters: params,
        },
      );

      if (result?.errorMessage) {
        alert(`Image processing failed: ${result.errorMessage}`);
        return;
      }

      if (result?.assetMetadata) {
        setAsset(result.assetMetadata);
        // Bust any cached local preview by appending timestamp
        setLocalPreviewUrl(
          `${result.assetMetadata.original.url}?t=${Date.now()}`,
        );
        onChange?.(result.assetMetadata.original as never);
      }

      setEditorOpen(false);
      resetEditorState();
    } finally {
      setApplying(false);
    }
  }, [
    asset,
    cropSelection,
    rotation,
    flipH,
    flipV,
    brightness,
    contrast,
    saturation,
    filterPreset,
    outputFormat,
    quality,
    executeCommand,
    props,
    onChange,
    resetEditorState,
  ]);

  /* ─── Crop interaction handlers ─── */

  const handleCropMouseDown = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      if (!cropEnabled || !cropRef.current) return;
      const rect = cropRef.current.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;
      cropStartRef.current = { startX: x, startY: y };
      setCropSelection({ x, y, width: 0, height: 0 });
    },
    [cropEnabled],
  );

  const handleCropMouseMove = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      if (!cropStartRef.current || !cropRef.current) return;
      const rect = cropRef.current.getBoundingClientRect();
      const currentX = Math.max(0, Math.min(e.clientX - rect.left, rect.width));
      const currentY = Math.max(0, Math.min(e.clientY - rect.top, rect.height));
      const { startX, startY } = cropStartRef.current;

      setCropSelection({
        x: Math.min(startX, currentX),
        y: Math.min(startY, currentY),
        width: Math.abs(currentX - startX),
        height: Math.abs(currentY - startY),
      });
    },
    [],
  );

  const handleCropMouseUp = useCallback(() => {
    cropStartRef.current = null;
  }, []);

  // Convert crop selection from display pixels to source image pixels
  const scaledCrop = useMemo(() => {
    if (!cropSelection || !asset || !cropRef.current) return null;
    const rect = cropRef.current.getBoundingClientRect();
    const scaleX = (asset.original.width || rect.width) / rect.width;
    const scaleY = (asset.original.height || rect.height) / rect.height;
    return {
      x: cropSelection.x * scaleX,
      y: cropSelection.y * scaleY,
      width: cropSelection.width * scaleX,
      height: cropSelection.height * scaleY,
    };
  }, [cropSelection, asset]);

  // Update cropSelection state accessible to handleApplyEdits with scaled values
  useEffect(() => {
    if (scaledCrop && cropSelection && cropSelection.width > 5) {
      // Store the scaled crop for the apply handler
      // We'll compute it fresh in handleApplyEdits via the ref
    }
  }, [scaledCrop, cropSelection]);

  const previewFilterCss = useMemo(
    () => cssFilterString(brightness, contrast, saturation, filterPreset),
    [brightness, contrast, saturation, filterPreset],
  );

  /* ─── Render: Drop zone ─── */

  const acceptString = buildAcceptString(allowedExtensions);

  const renderDropZone = () => (
    <FileDropOverlay
      onDrop={uploadFiles}
      onActiveChange={() => undefined}
      disabled={isReadOnly || isUploading}
    >
      <div
        style={{
          border: '1px dashed var(--color-input-border)',
          borderRadius: '20px',
          padding: Spacing.L,
          minWidth: '100%',
          background: 'var(--color-paper-background)',
        }}
      >
        <Stack align={LayoutAlignment.Center} spacing={Spacing.S}>
          {isUploading ? (
            <>
              <span>Uploading… {uploadState.progress}%</span>
              <div style={{ width: '100%' }}>
                <ProgressBar completed={uploadState.progress} />
              </div>
              <Button
                label="Cancel"
                color={ButtonColor.Secondary}
                size={ButtonSize.S}
                onClick={handleCancelUpload}
              />
            </>
          ) : generating ? (
            <Stack align={LayoutAlignment.Center} spacing={Spacing.M}>
              <Spinner />
              <span style={{ fontWeight: 500 }}>Generating image with AI…</span>
              <span style={{ fontSize: '12px', opacity: 0.6 }}>
                This may take a few seconds
              </span>
            </Stack>
          ) : (
            <>
              {uploadState.phase === 'error' && (
                <Callout
                  type={CalloutType.FriendlyWarning}
                  placement={CalloutPlacementType.OnPaper}
                  headline="Upload error"
                >
                  {uploadState.errorMessage}
                </Callout>
              )}
              <BrowseButton
                label="Upload image"
                accept={acceptString}
                allowMultipleFiles={false}
                disabled={isReadOnly}
                onUpload={uploadFiles}
              />
              <span>
                or drop a file here —{' '}
                {allowedExtensions.map((e) => e.toUpperCase()).join(', ')}
              </span>
              {aiGenerationAvailable && !isReadOnly && (
                <>
                  <Divider orientation={DividerOrientation.Horizontal} />
                  <Button
                    label="✨ Generate with AI"
                    color={ButtonColor.Secondary}
                    size={ButtonSize.S}
                    onClick={openAiPanel}
                  />
                </>
              )}
            </>
          )}
        </Stack>
      </div>
    </FileDropOverlay>
  );

  /* ─── Render: AI generate panel (Dialog) ─── */

  const closeAiPanel = useCallback(() => {
    setShowAiPanel(false);
    setAiError(undefined);
  }, []);

  const clearReferenceImage = useCallback(() => {
    setReferenceImageFile(null);
    setReferenceImagePreview(undefined);
    if (refImageInputRef.current) {
      refImageInputRef.current.value = '';
    }
  }, []);

  const renderAiPanel = () => (
    <Dialog
      isOpen={showAiPanel}
      headline="AI Image Generation"
      onClose={closeAiPanel}
      headerCloseButton={{ tooltipText: 'Close' }}
      isDismissable={!generating}
      width={480}
      confirmAction={{
        label: generating ? 'Generating…' : 'Generate Image',
        onClick: () => void handleGenerateImage(),
        disabled: generating || !aiPrompt.trim(),
        inProgress: generating,
      }}
      cancelAction={{
        label: 'Cancel',
        onClick: closeAiPanel,
        disabled: generating,
      }}
    >
      <Stack spacing={Spacing.L}>
        <TextArea
          label="Prompt"
          placeholder="Describe the image you want to generate…"
          value={aiPrompt}
          onChange={(e) => setAiPrompt(e.currentTarget.value)}
          minRows={3}
          maxRows={6}
          markAsRequired
        />

        <TextArea
          label="Negative prompt"
          placeholder="What to avoid (optional)…"
          value={aiNegativePrompt}
          onChange={(e) => setAiNegativePrompt(e.currentTarget.value)}
          minRows={2}
          maxRows={4}
          explanationText="Describe elements you don't want in the generated image"
        />

        {/* Reference image */}
        <Stack spacing={Spacing.XS}>
          <Headline size={HeadlineSize.S}>Reference image</Headline>
          <span style={{ fontSize: '12px', opacity: 0.6 }}>
            Optional — guides style, composition, or subject
          </span>
          {referenceImagePreview ? (
            <div
              style={{
                position: 'relative',
                display: 'inline-block',
                borderRadius: 'var(--border-radius-m, 6px)',
                overflow: 'hidden',
                border: '1px solid var(--color-border-default)',
                maxWidth: '160px',
              }}
            >
              <img
                src={referenceImagePreview}
                alt="Reference"
                style={{
                  display: 'block',
                  maxWidth: '160px',
                  maxHeight: '120px',
                  objectFit: 'cover',
                }}
              />
              <Button
                label="✕"
                color={ButtonColor.Quinary}
                size={ButtonSize.XS}
                onClick={clearReferenceImage}
              />
            </div>
          ) : (
            <Button
              label="Upload reference image"
              icon="xp-image"
              color={ButtonColor.Secondary}
              size={ButtonSize.S}
              onClick={() => refImageInputRef.current?.click()}
            />
          )}
          <input
            ref={refImageInputRef}
            type="file"
            title="Upload reference image"
            accept="image/png,image/jpeg,image/webp,image/gif"
            style={{ display: 'none' }}
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) {
                setReferenceImageFile(file);
                setReferenceImagePreview(URL.createObjectURL(file));
              }
            }}
          />
        </Stack>

        <Select
          label="Aspect ratio"
          value={aiAspectRatio}
          onChange={(val) => setAiAspectRatio(val ?? '1:1')}
        >
          {aspectRatioOptions.map((o) => (
            <MenuItem
              key={o.value}
              primaryLabel={o.label}
              value={o.value}
              selected={aiAspectRatio === o.value}
              onClick={() => setAiAspectRatio(o.value)}
            />
          ))}
        </Select>

        {aiError && (
          <Callout
            type={CalloutType.FriendlyWarning}
            placement={CalloutPlacementType.OnPaper}
            headline="Generation failed"
          >
            {aiError}
          </Callout>
        )}
      </Stack>
    </Dialog>
  );

  /* ─── Render: Asset tile (preview mode) ─── */

  const tileActions = asset
    ? [
        ...(!isReadOnly
          ? [
              {
                icon: 'xp-adjust' as const,
                title: 'Adjust image',
                onClick: openAdjustDialog,
              },
              {
                icon: 'xp-edit' as const,
                title: 'Edit Image',
                onClick: openEditor,
              },
            ]
          : []),
        ...(!isReadOnly && aiGenerationAvailable
          ? [
              {
                icon: 'xp-magic-edit' as const,
                title: 'Generate with AI',
                onClick: openAiPanel,
              },
            ]
          : []),
        {
          icon: 'xp-chain' as const,
          title: 'Copy link',
          onClick: () => {
            const fullUrl = asset.original.url.startsWith('http')
              ? asset.original.url
              : `${window.location.origin}${asset.original.url}`;
            void navigator.clipboard.writeText(fullUrl);
          },
        },
        {
          icon: 'xp-arrow-down-line' as const,
          title: 'Download',
          onClick: () => {
            const a = document.createElement('a');
            a.href = asset.original.url;
            a.download = asset.original.name;
            a.click();
          },
        },
        ...(!isReadOnly
          ? [
              {
                icon: 'xp-bin' as const,
                title: 'Delete',
                onClick: handleRemove,
                disabled: isUploading,
              },
            ]
          : []),
      ]
    : [];

  const renderAssetView = () => {
    if (!asset) return null;
    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: Spacing.S }}>
        <AssetTilePreview
          name={asset.original.name}
          disabled={isReadOnly || isUploading}
          size={asset.original.size}
          actions={tileActions}
          url={imageSrc}
          onClick={openEditor}
        />
        {!isReadOnly && !isUploading && (
          <BrowseButton
            label="Replace"
            accept={acceptString}
            allowMultipleFiles={false}
            disabled={isReadOnly}
            onUpload={uploadFiles}
          />
        )}
      </div>
    );
  };

  /* ─── Render: Editor dialog (fullscreen Kentico Dialog) ─── */

  const renderEditor = () => {
    if (!editorOpen || !imageSrc) return null;

    return (
      <Dialog
        isOpen={editorOpen}
        headline={`Edit Image — ${asset?.original.name ?? ''}`}
        onClose={closeEditor}
        headerCloseButton={{ tooltipText: 'Close editor' }}
        isDismissable={!applying}
        isFullScreen
        actionInProgress={applying}
        confirmAction={{
          label: applying ? 'Applying…' : 'Apply Changes',
          onClick: () => void handleApplyEdits(),
          disabled: applying,
          inProgress: applying,
        }}
        cancelAction={{
          label: 'Cancel',
          onClick: closeEditor,
          disabled: applying,
        }}
        secondaryAction={{
          label: 'Reset All',
          onClick: resetEditorState,
          disabled: applying,
        }}
      >
        <div
          style={{
            display: 'flex',
            flex: 1,
            overflow: 'hidden',
            height: '100%',
          }}
        >
          {/* Canvas area */}
          <div
            style={{
              flex: 1,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              position: 'relative',
              overflow: 'auto',
              padding: Spacing.XL,
              background: 'var(--color-background-default)',
            }}
          >
            <div
              ref={cropRef}
              style={{ position: 'relative', display: 'inline-block' }}
              onMouseDown={handleCropMouseDown}
              onMouseMove={handleCropMouseMove}
              onMouseUp={handleCropMouseUp}
              onMouseLeave={handleCropMouseUp}
            >
              <img
                src={imageSrc}
                alt="Edit preview"
                style={{
                  maxWidth: '100%',
                  maxHeight: 'calc(100vh - 180px)',
                  objectFit: 'contain',
                  filter: previewFilterCss,
                  transform: `rotate(${rotation}deg) scaleX(${flipH ? -1 : 1}) scaleY(${flipV ? -1 : 1})`,
                  transition: 'filter 0.15s, transform 0.15s',
                  userSelect: 'none',
                  cursor: cropEnabled ? 'crosshair' : 'default',
                }}
                draggable={false}
              />
              {/* Crop overlay */}
              {cropEnabled && cropSelection && cropSelection.width > 2 && (
                <>
                  <div
                    style={{
                      position: 'absolute',
                      top: 0,
                      left: 0,
                      right: 0,
                      bottom: 0,
                      background: 'rgba(0,0,0,0.5)',
                      pointerEvents: 'none',
                      clipPath: `polygon(
                        0% 0%, 100% 0%, 100% 100%, 0% 100%,
                        0% 0%,
                        ${cropSelection.x}px ${cropSelection.y}px,
                        ${cropSelection.x}px ${cropSelection.y + cropSelection.height}px,
                        ${cropSelection.x + cropSelection.width}px ${cropSelection.y + cropSelection.height}px,
                        ${cropSelection.x + cropSelection.width}px ${cropSelection.y}px,
                        ${cropSelection.x}px ${cropSelection.y}px
                      )`,
                    }}
                  />
                  <div
                    style={{
                      position: 'absolute',
                      left: cropSelection.x,
                      top: cropSelection.y,
                      width: cropSelection.width,
                      height: cropSelection.height,
                      border: '2px dashed #fff',
                      boxShadow: '0 0 0 1px rgba(0,0,0,0.3)',
                      pointerEvents: 'none',
                    }}
                  />
                  {scaledCrop && (
                    <div
                      style={{
                        position: 'absolute',
                        left: cropSelection.x,
                        top: cropSelection.y - 22,
                        fontSize: '11px',
                        color: '#fff',
                        background: 'rgba(0,0,0,0.7)',
                        padding: '2px 6px',
                        borderRadius: '3px',
                        pointerEvents: 'none',
                      }}
                    >
                      {Math.round(scaledCrop.width)} ×{' '}
                      {Math.round(scaledCrop.height)}
                    </div>
                  )}
                </>
              )}
            </div>
          </div>

          {/* Sidebar */}
          <div
            style={{
              width: '280px',
              borderLeft: '1px solid var(--color-border-default)',
              overflowY: 'auto',
              background: 'var(--color-paper-background)',
              padding: Spacing.L,
            }}
          >
            <Stack spacing={Spacing.L}>
              {/* Crop */}
              <Stack spacing={Spacing.S}>
                <Headline size={HeadlineSize.S}>Crop</Headline>
                <Switch
                  value={cropEnabled}
                  size={SwitchSize.M}
                  label="Enable crop tool"
                  onChange={(val) => {
                    setCropEnabled(val);
                    if (!val) setCropSelection(null);
                  }}
                />
                {cropEnabled && (
                  <span style={{ fontSize: '11px', opacity: 0.6 }}>
                    Click and drag on the image to select crop region
                  </span>
                )}
                {cropEnabled && cropSelection && cropSelection.width > 5 && (
                  <Button
                    label="Clear selection"
                    color={ButtonColor.Secondary}
                    size={ButtonSize.S}
                    onClick={() => setCropSelection(null)}
                  />
                )}
              </Stack>

              <Divider orientation={DividerOrientation.Horizontal} />

              {/* Transform */}
              <Stack spacing={Spacing.S}>
                <Headline size={HeadlineSize.S}>Transform</Headline>
                <Inline>
                  <Button
                    label="↻ 90°"
                    color={ButtonColor.Secondary}
                    size={ButtonSize.XS}
                    onClick={() => setRotation((r) => (r + 90) % 360)}
                  />
                  <Button
                    label="↺ 90°"
                    color={ButtonColor.Secondary}
                    size={ButtonSize.XS}
                    onClick={() => setRotation((r) => (r + 270) % 360)}
                  />
                  <Button
                    label="⇔ Flip H"
                    color={flipH ? ButtonColor.Primary : ButtonColor.Secondary}
                    size={ButtonSize.XS}
                    onClick={() => setFlipH((v) => !v)}
                  />
                  <Button
                    label="⇕ Flip V"
                    color={flipV ? ButtonColor.Primary : ButtonColor.Secondary}
                    size={ButtonSize.XS}
                    onClick={() => setFlipV((v) => !v)}
                  />
                </Inline>
                {rotation !== 0 && (
                  <span style={{ fontSize: '11px', opacity: 0.6 }}>
                    Rotation: {rotation}°
                  </span>
                )}
              </Stack>

              <Divider orientation={DividerOrientation.Horizontal} />

              {/* Adjustments */}
              <Stack spacing={Spacing.S}>
                <Headline size={HeadlineSize.S}>Adjustments</Headline>
                <SliderControl
                  label="Brightness"
                  value={brightness}
                  min={0.2}
                  max={2.0}
                  step={0.05}
                  onChange={setBrightness}
                  defaultValue={1.0}
                />
                <SliderControl
                  label="Contrast"
                  value={contrast}
                  min={0.2}
                  max={2.0}
                  step={0.05}
                  onChange={setContrast}
                  defaultValue={1.0}
                />
                <SliderControl
                  label="Saturation"
                  value={saturation}
                  min={0}
                  max={2.0}
                  step={0.05}
                  onChange={setSaturation}
                  defaultValue={1.0}
                />
              </Stack>

              <Divider orientation={DividerOrientation.Horizontal} />

              {/* Filters */}
              <Stack spacing={Spacing.S}>
                <Headline size={HeadlineSize.S}>Filters</Headline>
                <Inline>
                  {filterPresets.map((f) => (
                    <Button
                      key={f}
                      label={f.charAt(0).toUpperCase() + f.slice(1)}
                      color={
                        filterPreset === f
                          ? ButtonColor.Primary
                          : ButtonColor.Secondary
                      }
                      size={ButtonSize.XS}
                      onClick={() => setFilterPreset(f)}
                    />
                  ))}
                </Inline>
              </Stack>
            </Stack>
          </div>
        </div>
      </Dialog>
    );
  };

  /* ─── Render: Adjust image dialog (Kentico Dialog) ─── */

  const qualityOptions = [
    { label: 'Low 50%', value: '50' },
    { label: 'Medium 70%', value: '70' },
    { label: 'Medium 80%', value: '80' },
    { label: 'High 90%', value: '90' },
    { label: 'Maximum 100%', value: '100' },
  ] as const;

  const renderAdjustDialog = () => {
    if (!asset) return null;

    const currentExt =
      asset.original.extension?.replace('.', '').toLowerCase() || '';
    const previewSrc =
      adjustPreviewUrl || localPreviewUrl || asset.original.url;
    const isLossless = adjustFormat === 'png';

    return (
      <Dialog
        isOpen={adjustDialogOpen}
        headline="Adjust image"
        onClose={closeAdjustDialog}
        headerCloseButton={{ tooltipText: 'Close' }}
        isDismissable={!adjustApplying}
        width={560}
        actionInProgress={adjustApplying}
        confirmAction={{
          label: adjustApplying ? 'Saving…' : 'Save',
          onClick: () => void handleApplyAdjust(),
          disabled: adjustApplying,
          inProgress: adjustApplying,
        }}
        cancelAction={{
          label: 'Cancel',
          onClick: closeAdjustDialog,
          disabled: adjustApplying,
        }}
      >
        <Stack spacing={Spacing.L}>
          {/* Image preview */}
          <Paper elevation={PaperElevation.XS}>
            <div
              style={{
                display: 'flex',
                justifyContent: 'center',
                padding: Spacing.L,
                maxHeight: '300px',
              }}
            >
              <img
                src={previewSrc}
                alt="Adjust preview"
                style={{
                  maxWidth: '100%',
                  maxHeight: '268px',
                  objectFit: 'contain',
                }}
              />
            </div>
          </Paper>

          {/* Image info */}
          <TextWithLabel
            label="Image info"
            value={`${asset.original.name} — ${asset.original.width}×${asset.original.height} px • ${(asset.original.size / 1024).toFixed(0)} KB • ${currentExt.toUpperCase()}`}
          />

          {/* Format */}
          <Select
            label="Image format"
            value={adjustFormat}
            onChange={(val) => setAdjustFormat(val ?? 'png')}
          >
            {formatOptions.map((f) => (
              <MenuItem
                key={f}
                primaryLabel={f === 'jpeg' ? 'JPEG' : f.toUpperCase()}
                value={f}
                selected={adjustFormat === f}
                onClick={() => setAdjustFormat(f)}
              />
            ))}
          </Select>

          {/* Quality */}
          <Select
            label={
              isLossless ? 'Quality (lossless — does not apply)' : 'Quality'
            }
            value={String(adjustQuality)}
            onChange={(val) => setAdjustQuality(Number(val ?? '90'))}
            disabled={isLossless}
          >
            {qualityOptions.map((q) => (
              <MenuItem
                key={q.value}
                primaryLabel={q.label}
                value={q.value}
                selected={String(adjustQuality) === q.value}
                onClick={() => setAdjustQuality(Number(q.value))}
              />
            ))}
          </Select>
        </Stack>
      </Dialog>
    );
  };

  /* ─── Root ─── */

  return (
    <>
      <FormItemWrapper
        label={label}
        explanationText={explanationText}
        labelIconTooltip={tooltip}
        invalid={invalid}
        validationMessage={validationMessage}
        markAsRequired={required}
        disabled={isReadOnly}
      >
        {asset ? renderAssetView() : renderDropZone()}
      </FormItemWrapper>
      {renderEditor()}
      {renderAdjustDialog()}
      {renderAiPanel()}
    </>
  );
};

/* ═══════════════════════════════════════════════════════════════
   Subcomponents
   ═══════════════════════════════════════════════════════════════ */

const SliderControl: React.FC<{
  label: string;
  value: number;
  min: number;
  max: number;
  step: number;
  onChange: (v: number) => void;
  defaultValue: number;
  unit?: string;
}> = ({ label, value, min, max, step, onChange, defaultValue, unit }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
    <div
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
      }}
    >
      <span style={{ fontSize: '12px' }}>{label}</span>
      <span style={{ fontSize: '11px', opacity: 0.6 }}>
        {typeof value === 'number' && value % 1 === 0
          ? value
          : value.toFixed(2)}
        {unit || ''}
        {value !== defaultValue && (
          <button
            type="button"
            onClick={() => onChange(defaultValue)}
            style={{
              marginLeft: '4px',
              fontSize: '10px',
              background: 'none',
              border: 'none',
              color: 'var(--color-primary, #4a9eff)',
              cursor: 'pointer',
              padding: 0,
            }}
          >
            reset
          </button>
        )}
      </span>
    </div>
    <input
      type="range"
      title={label}
      min={min}
      max={max}
      step={step}
      value={value}
      onChange={(e) => onChange(parseFloat(e.target.value))}
      style={{ width: '100%', accentColor: 'var(--color-primary, #4a9eff)' }}
    />
  </div>
);

/* ─── Export ─── */
export const ImageEditor = ImageEditorFormComponent;
