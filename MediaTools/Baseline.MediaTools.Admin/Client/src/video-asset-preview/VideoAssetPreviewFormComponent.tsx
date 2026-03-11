import React, { useCallback, useEffect, useRef, useState } from 'react';
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
  FileDropOverlay,
  FormItemWrapper,
  Spacing,
} from '@kentico/xperience-admin-components';

// ---------- Data shapes (mirror the C# ContentItemAssetMetadata* client items) ----------

interface AssetBaseClientItem {
  identifier: string;
  name: string;
  extension: string;
  size: number;
  width?: number;
  height?: number;
  isImage: boolean;
  isOptimizableImage: boolean;
  url: string;
  lastModified: string;
  errorMessage?: string;
}

interface AssetClientItem {
  original: AssetBaseClientItem;
  optimizationParameters?: unknown;
  variants?: unknown;
  focalPoint?: unknown;
}

// ---------- Command argument / result shapes ----------

interface UploadChunkArgs {
  chunkId: number;
  fileName: string;
  fileSize: number;
  fileIdentifier?: string;
}

interface UploadChunkResult {
  identifier: string;
  name: string;
  size: number;
  errorMessage?: string;
}

interface CompleteUploadArgs {
  fileName: string;
  fileIdentifier: string;
  fileSize: number;
}

interface CompleteUploadResult {
  assetMetadata: AssetClientItem;
}

interface RemoveUploadedFileArgs {
  assetMetadata?: AssetClientItem;
}

// ---------- Thumbnail command shapes ----------

interface SaveThumbnailArgs {
  thumbnailBase64: string;
}

interface SaveThumbnailResult {
  success: boolean;
}

// ---------- Component props (mirror VideoAssetPreviewClientProperties) ----------

interface VideoAssetPreviewFormComponentProps extends FormComponentProps {
  assetMetadata?: AssetClientItem;
  allowedExtensions?: string[];
  chunkSize?: number;
  publishedAssetUrl?: string;
}

// ---------- Upload state ----------

type UploadPhase = 'idle' | 'uploading' | 'error';

interface UploadState {
  phase: UploadPhase;
  progress: number;
  errorMessage?: string;
  abortController?: AbortController;
}

const defaultChunkSize = 4 * 1024 * 1024;
const defaultAllowedExtensions = ['mp4', 'webm', 'mov', 'avi', 'mkv'];

function buildAcceptString(extensions: string[]): string {
  return extensions.map((e) => `.${e}`).join(',');
}

function downloadFile(url: string, fileName: string): void {
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  a.click();
}

/**
 * VideoAssetPreviewFormComponent — mirrors Kentico's ContentItemAssetUploaderFormComponent
 * architecture exactly, substituting the image thumbnail with an inline HTML5 video player.
 *
 * Upload flow (identical to Kentico):
 *   1. File selected → split into chunkSize-byte pieces
 *   2. Each chunk → UploadChunk command (binary multipart)
 *   3. All chunks sent → CompleteUpload command → server returns AssetClientItem
 *   4. onChange(assetMetadata) updates the content item field value
 *
 * Actions (identical to Kentico image uploader):
 *   • Copy URL (when publishedAssetUrl is available)
 *   • Download
 *   • Delete (when editable)
 */
export const VideoAssetPreviewFormComponent: React.FC<
  VideoAssetPreviewFormComponentProps
> = (props) => {
  const {
    assetMetadata: initialAsset,
    allowedExtensions = defaultAllowedExtensions,
    chunkSize = defaultChunkSize,
    publishedAssetUrl,
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
  const abortControllerRef = useRef<AbortController | null>(null);

  const [asset, setAsset] = useState<AssetClientItem | undefined>(initialAsset);
  const [uploadState, setUploadState] = useState<UploadState>({
    phase: 'idle',
    progress: 0,
  });
  // Local object URL for immediate playback right after upload (before form save)
  const [localPreviewUrl, setLocalPreviewUrl] = useState<string | undefined>();

  const isReadOnly = disabled === true;
  const isUploading = uploadState.phase === 'uploading';

  const videoSrc = localPreviewUrl ?? (asset?.original.url || undefined);

  // ── Thumbnail generation ────────────────────────────────────────────────────
  // When a video asset exists and its URL is available, extract a frame from the
  // video (at ~1 s or midpoint) using a hidden <video> + <canvas>, encode it as
  // a JPEG, and send it to the server via the SaveThumbnail command.  The server
  // stores the thumbnail on disk so the Content Hub list view can show it.

  const thumbnailSentRef = useRef<string | null>(null);

  useEffect(() => {
    const identifier = asset?.original.identifier;
    if (!identifier || !videoSrc) return;
    // Only send once per identifier
    if (thumbnailSentRef.current === identifier) return;

    const vid = document.createElement('video');
    vid.crossOrigin = 'anonymous';
    vid.src = videoSrc;
    vid.muted = true;
    vid.preload = 'auto';

    const handleLoaded = () => {
      vid.currentTime = Math.min(1, vid.duration / 2);
    };

    const handleSeeked = () => {
      try {
        const maxWidth = 640;
        const scale = vid.videoWidth > maxWidth ? maxWidth / vid.videoWidth : 1;
        const w = Math.round(vid.videoWidth * scale);
        const h = Math.round(vid.videoHeight * scale);

        const canvas = document.createElement('canvas');
        canvas.width = w;
        canvas.height = h;
        const ctx = canvas.getContext('2d');
        if (!ctx) return;
        ctx.drawImage(vid, 0, 0, w, h);

        const dataUrl = canvas.toDataURL('image/jpeg', 0.8);
        const base64 = dataUrl.split(',')[1];
        if (!base64) return;

        thumbnailSentRef.current = identifier;

        executeCommand<SaveThumbnailResult, SaveThumbnailArgs>(
          props,
          'SaveThumbnail',
          { thumbnailBase64: base64 },
        ).catch(() => {
          // Reset so it retries on next render
          thumbnailSentRef.current = null;
        });
      } finally {
        vid.remove();
      }
    };

    vid.addEventListener('loadeddata', handleLoaded);
    vid.addEventListener('seeked', handleSeeked);

    return () => {
      vid.removeEventListener('loadeddata', handleLoaded);
      vid.removeEventListener('seeked', handleSeeked);
      vid.remove();
    };
  }, [asset?.original.identifier, videoSrc, executeCommand, props]);
  // ── Chunked upload ──────────────────────────────────────────────────────────

  const uploadFiles = useCallback(
    async (files: FileList) => {
      const file = files[0];
      if (!file) return;

      const abort = new AbortController();
      abortControllerRef.current = abort;
      setUploadState({
        phase: 'uploading',
        progress: 0,
        abortController: abort,
      });

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

          const chunkResult = await executeCommand<
            UploadChunkResult,
            UploadChunkArgs
          >(
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

          if (chunkResult?.errorMessage) {
            setUploadState({
              phase: 'error',
              progress: 0,
              errorMessage: chunkResult.errorMessage,
            });
            return;
          }

          if (!fileIdentifier && chunkResult?.identifier) {
            fileIdentifier = chunkResult.identifier;
          }

          setUploadState((prev) => ({
            ...prev,
            progress: Math.round(((chunkId + 1) / totalChunks) * 100),
          }));
        }

        if (!fileIdentifier) {
          setUploadState({
            phase: 'error',
            progress: 0,
            errorMessage: 'Upload failed: no identifier received.',
          });
          return;
        }

        const completeResult = await executeCommand<
          CompleteUploadResult,
          CompleteUploadArgs
        >(
          props,
          'CompleteUpload',
          { fileName: file.name, fileIdentifier, fileSize: file.size },
          undefined,
          abort,
        );

        if (completeResult?.assetMetadata) {
          setAsset(completeResult.assetMetadata);
          setLocalPreviewUrl(URL.createObjectURL(file));
          onChange?.(completeResult.assetMetadata.original as never);
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

  // ── Remove ──────────────────────────────────────────────────────────────────

  const handleRemove = useCallback(async () => {
    await executeCommand<void, RemoveUploadedFileArgs>(
      props,
      'RemoveUploadedFile',
      {
        assetMetadata: asset,
      },
    );
    setAsset(undefined);
    setLocalPreviewUrl(undefined);
    onChange?.(null as never);
  }, [asset, executeCommand, onChange, props]);

  const handleCancelUpload = useCallback(() => {
    abortControllerRef.current?.abort();
    setUploadState({ phase: 'idle', progress: 0 });
  }, []);

  // ── Action buttons (same set as Kentico image uploader) ─────────────────────

  const tileActions = asset
    ? [
        ...(publishedAssetUrl
          ? [
              {
                icon: 'xp-chain' as const,
                title: 'Copy URL',
                onClick: () =>
                  navigator.clipboard.writeText(
                    `${location.origin}/${publishedAssetUrl}`,
                  ),
              },
            ]
          : []),
        {
          icon: 'xp-arrow-down-line' as const,
          title: 'Download',
          onClick: () => downloadFile(asset.original.url, asset.original.name),
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

  // ── Drop zone (shown when no asset) ─────────────────────────────────────────

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
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          flexDirection: 'column',
          gap: Spacing.S,
          textAlign: 'center',
          color: 'var(--color-text-default-on-light)',
          fontSize: '14px',
        }}
      >
        {isUploading ? (
          <>
            <span style={{ fontSize: '14px' }}>
              Uploading… {uploadState.progress}%
            </span>
            <div
              style={{
                width: '100%',
                height: '4px',
                background: 'var(--color-border-default)',
                borderRadius: '2px',
              }}
            >
              <div
                style={{
                  width: `${uploadState.progress}%`,
                  height: '100%',
                  background: 'var(--color-primary)',
                  borderRadius: '2px',
                  transition: 'width 0.2s',
                }}
              />
            </div>
            <Button
              label="Cancel"
              color={ButtonColor.Secondary}
              size={ButtonSize.S}
              onClick={handleCancelUpload}
            />
          </>
        ) : (
          <>
            {uploadState.phase === 'error' && (
              <span style={{ color: 'var(--color-alert-icon)' }}>
                {uploadState.errorMessage}
              </span>
            )}
            <BrowseButton
              label="Upload video"
              accept={acceptString}
              allowMultipleFiles={false}
              disabled={isReadOnly}
              onUpload={uploadFiles}
            />
            <span>
              or drop a file here — {allowedExtensions.join(', ').toUpperCase()}
            </span>
          </>
        )}
      </div>
    </FileDropOverlay>
  );

  // ── Asset tile + video preview (shown when asset exists) ─────────────────────

  const tileRef = useRef<HTMLDivElement>(null);
  const videoRef = useRef<HTMLVideoElement | null>(null);

  // Inject a <video> element into the AssetTilePreview's image-preview area
  // so we get the native Kentico tile appearance with a real video thumbnail.
  useEffect(() => {
    const container = tileRef.current;
    if (!container || !videoSrc || !asset) return;

    // The AssetTilePreview renders its preview image inside a div with
    // data-testid="image-preview" or the first child wrapper of the tile.
    const previewEl =
      container.querySelector('[data-testid="image-preview"]') ??
      container.querySelector('button > div:first-child');
    if (!previewEl) return;

    // Clear any existing content (the default file-type icon)
    previewEl.innerHTML = '';

    // Create video element
    const vid = document.createElement('video');
    vid.src = videoSrc;
    vid.muted = true;
    vid.playsInline = true;
    vid.preload = 'metadata';
    vid.loop = true;
    vid.style.width = '100%';
    vid.style.height = '100%';
    vid.style.objectFit = 'cover';
    vid.style.display = 'block';
    vid.style.borderRadius = '8px 8px 0 0';

    // Store ref for hover handlers
    videoRef.current = vid;

    previewEl.appendChild(vid);

    // Play/pause on hover over the entire tile
    const handleEnter = () => {
      vid.currentTime = 0;
      vid.play().catch(() => {
        /* autoplay may be blocked */
      });
    };
    const handleLeave = () => {
      vid.pause();
      vid.currentTime = 0;
    };

    container.addEventListener('mouseenter', handleEnter);
    container.addEventListener('mouseleave', handleLeave);

    return () => {
      container.removeEventListener('mouseenter', handleEnter);
      container.removeEventListener('mouseleave', handleLeave);
      videoRef.current = null;
    };
  }, [videoSrc, asset]);

  const renderAssetView = () => {
    if (!asset) return null;
    const { original } = asset;
    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: Spacing.S }}>
        <div ref={tileRef}>
          <AssetTilePreview
            name={original.name}
            disabled={isReadOnly || isUploading}
            size={original.size}
            actions={tileActions}
            /* Provide a transparent 1x1 GIF so the tile renders the full
               preview area + toolbar overlay (same as image tiles). Our
               useEffect then replaces the <img> with a <video>. */
            url="data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"
            onClick={() => {
              /* noop — keeps tile as <button> so toolbar icons render */
            }}
          />
        </div>
      </div>
    );
  };

  // ── Root ────────────────────────────────────────────────────────────────────

  return (
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
  );
};

export const VideoAssetPreview = VideoAssetPreviewFormComponent;
