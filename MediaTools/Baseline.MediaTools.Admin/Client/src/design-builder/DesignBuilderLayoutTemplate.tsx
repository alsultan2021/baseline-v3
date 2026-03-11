/* DesignBuilderLayout – PosterMyWall-inspired design builder for Kentico admin */
import React, { useCallback, useEffect, useRef, useState } from 'react';
import { usePageCommand } from '@kentico/xperience-admin-base';

/* ─── Types ────────────────────────────────────────────────────── */

type CanvasSizePreset = {
  name: string;
  category: string;
  width: number;
  height: number;
};

type DesignTemplate = {
  id: string;
  name: string;
  category: string;
  previewUrl: string;
  designJson: string;
};

type MediaAsset = {
  id: string;
  name: string;
  url: string;
  type: string;
  width: number;
  height: number;
};

type DesignSummary = {
  id: number;
  name: string;
  previewUrl: string;
  lastModified: string;
  width: number;
  height: number;
};

type DesignElement = {
  id: string;
  type: 'text' | 'rect' | 'circle' | 'image' | 'video' | 'line' | 'audio';
  x: number;
  y: number;
  width?: number;
  height?: number;
  radius?: number;
  text?: string;
  fontSize?: number;
  fontWeight?: string;
  fontFamily?: string;
  fill: string;
  stroke?: string;
  strokeWidth?: number;
  opacity?: number;
  rotation?: number;
  letterSpacing?: number;
  src?: string;
  locked?: boolean;
  videoUrl?: string;
  autoplay?: boolean;
  loop?: boolean;
  muted?: boolean;
  // Line properties
  strokeStyle?: 'solid' | 'dashed' | 'dotted';
  lineCap?: 'butt' | 'round' | 'square';
  arrowStart?: boolean;
  arrowEnd?: boolean;
  // Audio properties
  audioUrl?: string;
  volume?: number;
  fadeIn?: number;
  fadeOut?: number;
  startTime?: number;
  duration?: number;
};

type DesignState = {
  backgroundColor: string;
  width: number;
  height: number;
  elements: DesignElement[];
};

interface DesignBuilderClientProperties {
  designs: DesignSummary[];
  currentDesignJson: string | null;
  currentDesignId: number;
  templates: DesignTemplate[];
  mediaAssets: MediaAsset[];
  sizePresets: CanvasSizePreset[];
}

type SidebarTab =
  | 'uploads'
  | 'templates'
  | 'media'
  | 'text'
  | 'shapes'
  | 'background'
  | 'layers'
  | 'layout'
  | 'audio'
  | 'draw';

/* ─── Utility ──────────────────────────────────────────────────── */

const uid = () => `el-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;

/* ─── Built-in Templates ───────────────────────────────────────── */

const createBuiltInTemplates = (): DesignTemplate[] => {
  const templates: DesignTemplate[] = [];

  // Social Media Templates
  templates.push({
    id: 'social-instagram-1',
    name: 'Instagram Post - Bold',
    category: 'social media',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#FF6B6B',
      width: 1080,
      height: 1080,
      elements: [
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 1080,
          height: 1080,
          fill: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'text',
          x: 90,
          y: 400,
          text: 'YOUR TEXT HERE',
          fontSize: 72,
          fontWeight: 'bold',
          fill: '#ffffff',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 90,
          y: 520,
          text: 'Add your description',
          fontSize: 32,
          fontWeight: 'normal',
          fill: '#ffffff',
          opacity: 0.9,
          fontFamily: 'Inter, sans-serif',
        },
      ],
    }),
  });

  templates.push({
    id: 'social-instagram-2',
    name: 'Instagram Story',
    category: 'social media',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#ffffff',
      width: 1080,
      height: 1920,
      elements: [
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 1080,
          height: 1920,
          fill: '#f0f0f0',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'circle',
          x: 540,
          y: 400,
          radius: 250,
          fill: '#FF6B6B',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'text',
          x: 100,
          y: 800,
          text: 'STORY TITLE',
          fontSize: 64,
          fontWeight: 'bold',
          fill: '#333333',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
      ],
    }),
  });

  templates.push({
    id: 'social-facebook-1',
    name: 'Facebook Cover',
    category: 'social media',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#4267B2',
      width: 1640,
      height: 924,
      elements: [
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 1640,
          height: 924,
          fill: '#4267B2',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'text',
          x: 100,
          y: 350,
          text: 'YOUR BRAND',
          fontSize: 96,
          fontWeight: 'bold',
          fill: '#ffffff',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 100,
          y: 480,
          text: 'Tagline or description here',
          fontSize: 48,
          fontWeight: 'normal',
          fill: '#ffffff',
          opacity: 0.9,
          fontFamily: 'Inter, sans-serif',
        },
      ],
    }),
  });

  // Event Templates
  templates.push({
    id: 'event-birthday-1',
    name: 'Birthday Invitation',
    category: 'events',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#FFE5E5',
      width: 1080,
      height: 1080,
      elements: [
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 1080,
          height: 1080,
          fill: '#FFE5E5',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'text',
          x: 540,
          y: 400,
          text: '🎉',
          fontSize: 120,
          fill: '#FF6B6B',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 200,
          y: 580,
          text: "YOU'RE INVITED!",
          fontSize: 64,
          fontWeight: 'bold',
          fill: '#333333',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 200,
          y: 680,
          text: 'Birthday Celebration',
          fontSize: 36,
          fill: '#666666',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
      ],
    }),
  });

  templates.push({
    id: 'event-wedding-1',
    name: 'Wedding Invitation',
    category: 'events',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#F5F5DC',
      width: 1080,
      height: 1350,
      elements: [
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 1080,
          height: 1350,
          fill: '#F5F5DC',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'text',
          x: 200,
          y: 400,
          text: 'Save the Date',
          fontSize: 72,
          fontWeight: '300',
          fill: '#8B7355',
          opacity: 1,
          fontFamily: 'Georgia, serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 200,
          y: 550,
          text: 'John & Jane',
          fontSize: 56,
          fontWeight: 'bold',
          fill: '#8B7355',
          opacity: 1,
          fontFamily: 'Georgia, serif',
        },
        {
          id: uid(),
          type: 'line',
          x: 200,
          y: 650,
          width: 680,
          height: 2,
          fill: '#8B7355',
          opacity: 0.5,
        },
      ],
    }),
  });

  // Business Templates
  templates.push({
    id: 'business-flyer-1',
    name: 'Business Flyer',
    category: 'business',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#ffffff',
      width: 1080,
      height: 1080,
      elements: [
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 1080,
          height: 400,
          fill: '#0078d4',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'text',
          x: 80,
          y: 150,
          text: 'BUSINESS',
          fontSize: 80,
          fontWeight: 'bold',
          fill: '#ffffff',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 80,
          y: 250,
          text: 'Professional Services',
          fontSize: 36,
          fill: '#ffffff',
          opacity: 0.9,
          fontFamily: 'Inter, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 80,
          y: 520,
          text: 'Contact us today',
          fontSize: 48,
          fontWeight: '600',
          fill: '#333333',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
      ],
    }),
  });

  templates.push({
    id: 'business-card-1',
    name: 'Business Card',
    category: 'business',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#1a1a1a',
      width: 1050,
      height: 600,
      elements: [
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 1050,
          height: 600,
          fill: '#1a1a1a',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 8,
          height: 600,
          fill: '#0078d4',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'text',
          x: 60,
          y: 180,
          text: 'JOHN DOE',
          fontSize: 52,
          fontWeight: 'bold',
          fill: '#ffffff',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 60,
          y: 260,
          text: 'Chief Executive Officer',
          fontSize: 28,
          fill: '#cccccc',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 60,
          y: 380,
          text: 'contact@company.com',
          fontSize: 24,
          fill: '#0078d4',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
      ],
    }),
  });

  // Marketing Templates
  templates.push({
    id: 'marketing-promo-1',
    name: 'Sale Promotion',
    category: 'marketing',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#FF3B3B',
      width: 1080,
      height: 1080,
      elements: [
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 1080,
          height: 1080,
          fill: '#FF3B3B',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'circle',
          x: 540,
          y: 540,
          radius: 350,
          fill: '#ffffff',
          opacity: 0.15,
        },
        {
          id: uid(),
          type: 'text',
          x: 300,
          y: 400,
          text: '50% OFF',
          fontSize: 96,
          fontWeight: 'bold',
          fill: '#ffffff',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 250,
          y: 550,
          text: 'LIMITED TIME OFFER',
          fontSize: 40,
          fontWeight: '600',
          fill: '#ffffff',
          opacity: 0.95,
          fontFamily: 'Inter, sans-serif',
        },
      ],
    }),
  });

  templates.push({
    id: 'marketing-newsletter-1',
    name: 'Newsletter Header',
    category: 'marketing',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#f8f9fa',
      width: 1200,
      height: 600,
      elements: [
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 1200,
          height: 600,
          fill: '#f8f9fa',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 600,
          height: 600,
          fill: '#0078d4',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'text',
          x: 80,
          y: 220,
          text: 'NEWSLETTER',
          fontSize: 64,
          fontWeight: 'bold',
          fill: '#ffffff',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 80,
          y: 320,
          text: 'Monthly Update',
          fontSize: 36,
          fill: '#ffffff',
          opacity: 0.9,
          fontFamily: 'Inter, sans-serif',
        },
      ],
    }),
  });

  // Blank Templates
  templates.push({
    id: 'blank-square',
    name: 'Blank Square',
    category: 'blank',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#ffffff',
      width: 1080,
      height: 1080,
      elements: [],
    }),
  });

  templates.push({
    id: 'blank-landscape',
    name: 'Blank Landscape',
    category: 'blank',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#ffffff',
      width: 1920,
      height: 1080,
      elements: [],
    }),
  });

  templates.push({
    id: 'blank-portrait',
    name: 'Blank Portrait',
    category: 'blank',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#ffffff',
      width: 1080,
      height: 1920,
      elements: [],
    }),
  });

  // Poster Templates
  templates.push({
    id: 'poster-concert-1',
    name: 'Concert Poster',
    category: 'posters',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#0a0a0a',
      width: 1080,
      height: 1440,
      elements: [
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 1080,
          height: 1440,
          fill: '#0a0a0a',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 1080,
          height: 600,
          fill: '#FF006E',
          opacity: 0.3,
        },
        {
          id: uid(),
          type: 'text',
          x: 100,
          y: 500,
          text: 'LIVE MUSIC',
          fontSize: 88,
          fontWeight: 'bold',
          fill: '#ffffff',
          opacity: 1,
          fontFamily: 'Impact, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 100,
          y: 650,
          text: 'CONCERT',
          fontSize: 72,
          fontWeight: 'bold',
          fill: '#FF006E',
          opacity: 1,
          fontFamily: 'Impact, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 100,
          y: 850,
          text: 'Featuring: Artist Name',
          fontSize: 36,
          fill: '#cccccc',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
      ],
    }),
  });

  templates.push({
    id: 'poster-movie-1',
    name: 'Movie Poster',
    category: 'posters',
    previewUrl: '',
    designJson: JSON.stringify({
      backgroundColor: '#000000',
      width: 1080,
      height: 1620,
      elements: [
        {
          id: uid(),
          type: 'rect',
          x: 0,
          y: 0,
          width: 1080,
          height: 1620,
          fill: '#000000',
          opacity: 1,
        },
        {
          id: uid(),
          type: 'text',
          x: 100,
          y: 1200,
          text: 'MOVIE TITLE',
          fontSize: 96,
          fontWeight: 'bold',
          fill: '#ffffff',
          opacity: 1,
          fontFamily: 'Impact, sans-serif',
        },
        {
          id: uid(),
          type: 'text',
          x: 100,
          y: 1350,
          text: 'Coming Soon',
          fontSize: 48,
          fill: '#FFD700',
          opacity: 1,
          fontFamily: 'Inter, sans-serif',
        },
      ],
    }),
  });

  return templates;
};

/* ─── SVG Icon Components (PosterMyWall-style line icons) ───── */

const SvgIcon = ({
  d,
  size = 22,
  stroke = 'currentColor',
}: {
  d: string;
  size?: number;
  stroke?: string;
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    stroke={stroke}
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d={d} />
  </svg>
);

const IconUpload = () => (
  <svg
    width="22"
    height="22"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
    <polyline points="17 8 12 3 7 8" />
    <line x1="12" y1="3" x2="12" y2="15" />
  </svg>
);

const IconTemplates = () => (
  <svg
    width="22"
    height="22"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <rect x="3" y="3" width="7" height="7" />
    <rect x="14" y="3" width="7" height="7" />
    <rect x="3" y="14" width="7" height="7" />
    <rect x="14" y="14" width="7" height="7" />
  </svg>
);

const IconMedia = () => (
  <svg
    width="22"
    height="22"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <circle cx="9" cy="9" r="2" />
    <path d="M13 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-7" />
    <path d="m21 15-3.09-3.09a2 2 0 0 0-2.82 0L6 21" />
  </svg>
);

const IconText = () => (
  <svg
    width="22"
    height="22"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <polyline points="4 7 4 4 20 4 20 7" />
    <line x1="9" y1="20" x2="15" y2="20" />
    <line x1="12" y1="4" x2="12" y2="20" />
  </svg>
);

const IconShapes = () => (
  <svg
    width="22"
    height="22"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d="M12 2l9 17H3z" />
    <circle cx="12" cy="14" r="4" />
  </svg>
);

const IconBackground = () => (
  <svg
    width="22"
    height="22"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d="M2 6s1.5-2 5-2 5 2 5 2 1.5-2 5-2 5 2 5 2v14s-1.5-2-5-2-5 2-5 2-1.5-2-5-2-5 2-5 2V6z" />
  </svg>
);

const IconLayers = () => (
  <svg
    width="22"
    height="22"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <polygon points="12 2 2 7 12 12 22 7 12 2" />
    <polyline points="2 17 12 22 22 17" />
    <polyline points="2 12 12 17 22 12" />
  </svg>
);

const IconLayout = () => (
  <svg
    width="22"
    height="22"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <rect x="3" y="3" width="18" height="18" rx="2" />
    <line x1="3" y1="9" x2="21" y2="9" />
    <line x1="9" y1="21" x2="9" y2="9" />
  </svg>
);

const IconDraw = () => (
  <svg
    width="22"
    height="22"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d="M17 3a2.85 2.85 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z" />
  </svg>
);

const IconAudio = () => (
  <svg
    width="22"
    height="22"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d="M9 18V5l12-2v13" />
    <circle cx="6" cy="18" r="3" />
    <circle cx="18" cy="16" r="3" />
  </svg>
);

const IconUndo = () => (
  <SvgIcon d="M3 10h10a5 5 0 0 1 0 10H3M3 10l4-4M3 10l4 4" size={18} />
);
const IconRedo = () => (
  <SvgIcon d="M21 10H11a5 5 0 0 0 0 10h10M21 10l-4-4M21 10l-4 4" size={18} />
);
const IconBack = () => <SvgIcon d="M19 12H5M5 12l7 7M5 12l7-7" size={18} />;
const IconHelp = () => (
  <svg
    width="18"
    height="18"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <circle cx="12" cy="12" r="10" />
    <path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3" />
    <line x1="12" y1="17" x2="12.01" y2="17" />
  </svg>
);
const IconSave = () => (
  <svg
    width="16"
    height="16"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z" />
    <polyline points="17 21 17 13 7 13 7 21" />
    <polyline points="7 3 7 8 15 8" />
  </svg>
);
const IconShare = () => (
  <svg
    width="16"
    height="16"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <circle cx="18" cy="5" r="3" />
    <circle cx="6" cy="12" r="3" />
    <circle cx="18" cy="19" r="3" />
    <line x1="8.59" y1="13.51" x2="15.42" y2="17.49" />
    <line x1="15.41" y1="6.51" x2="8.59" y2="10.49" />
  </svg>
);
const IconDownload = () => (
  <svg
    width="16"
    height="16"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
    <polyline points="7 10 12 15 17 10" />
    <line x1="12" y1="15" x2="12" y2="3" />
  </svg>
);
const IconPublish = () => (
  <svg
    width="16"
    height="16"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <line x1="22" y1="2" x2="11" y2="13" />
    <polygon points="22 2 15 22 11 13 2 9 22 2" />
  </svg>
);
const IconZoomIn = () => (
  <svg
    width="20"
    height="20"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <circle cx="11" cy="11" r="8" />
    <line x1="21" y1="21" x2="16.65" y2="16.65" />
    <line x1="11" y1="8" x2="11" y2="14" />
    <line x1="8" y1="11" x2="14" y2="11" />
  </svg>
);
const IconZoomOut = () => (
  <svg
    width="20"
    height="20"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <circle cx="11" cy="11" r="8" />
    <line x1="21" y1="21" x2="16.65" y2="16.65" />
    <line x1="8" y1="11" x2="14" y2="11" />
  </svg>
);
const IconTimeline = () => (
  <svg
    width="16"
    height="16"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d="M17 3a2.85 2.85 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z" />
  </svg>
);
const IconMusic = () => (
  <svg
    width="16"
    height="16"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d="M9 18V5l12-2v13" />
    <circle cx="6" cy="18" r="3" />
    <circle cx="18" cy="16" r="3" />
  </svg>
);

const defaultDesign = (w = 1080, h = 1080): DesignState => ({
  backgroundColor: '#ffffff',
  width: w,
  height: h,
  elements: [],
});

/* ─── Sub-components: Sidebar Panels ──────────────────────────── */

const TemplatesPanel = ({
  templates,
  onSelect,
}: {
  templates: DesignTemplate[];
  onSelect: (t: DesignTemplate) => void;
}) => {
  const [search, setSearch] = useState('');
  const [filterCat, setFilterCat] = useState('all');
  const categories = [
    'all',
    ...Array.from(new Set(templates.map((t) => t.category))),
  ];
  const filtered = templates.filter((t) => {
    const matchSearch =
      !search || t.name.toLowerCase().includes(search.toLowerCase());
    const matchCat = filterCat === 'all' || t.category === filterCat;
    return matchSearch && matchCat;
  });

  return (
    <div style={panelStyles.container}>
      <h3 style={panelStyles.title}>Templates</h3>
      {/* Search bar */}
      <div style={{ marginBottom: 10 }}>
        <input
          type="text"
          placeholder="Search templates here"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          style={{
            width: '100%',
            padding: '8px 12px',
            border: '1px solid #e0e0e0',
            borderRadius: 8,
            fontSize: 12,
            boxSizing: 'border-box',
            background: '#fafafa',
          }}
        />
      </div>
      {/* Filter chips */}
      <div
        style={{ display: 'flex', gap: 6, marginBottom: 12, flexWrap: 'wrap' }}
      >
        {categories.map((cat) => (
          <button
            key={cat}
            onClick={() => setFilterCat(cat)}
            style={{
              padding: '5px 12px',
              fontSize: 11,
              borderRadius: 16,
              border: '1px solid',
              borderColor: filterCat === cat ? '#1976d2' : '#e0e0e0',
              background: filterCat === cat ? '#e8f4fd' : '#fff',
              color: filterCat === cat ? '#1976d2' : '#666',
              cursor: 'pointer',
              fontWeight: filterCat === cat ? 600 : 400,
              textTransform: 'capitalize',
            }}
          >
            {cat}
          </button>
        ))}
      </div>
      {/* Template grid */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
        {filtered.map((t) => (
          <button
            key={t.id}
            onClick={() => onSelect(t)}
            style={{
              display: 'flex',
              flexDirection: 'column',
              border: '1px solid #e8e8e8',
              borderRadius: 8,
              background: '#fff',
              cursor: 'pointer',
              padding: 0,
              overflow: 'hidden',
              transition: 'box-shadow 0.15s',
            }}
            title={t.name}
          >
            <div
              style={{
                width: '100%',
                height: 90,
                backgroundColor: '#f5f5f5',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                overflow: 'hidden',
              }}
            >
              {t.previewUrl ? (
                <img
                  src={t.previewUrl}
                  alt={t.name}
                  title={t.name}
                  style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                  loading="lazy"
                />
              ) : (
                <span style={{ fontSize: 28, color: '#ccc' }}>📄</span>
              )}
            </div>
            <div
              style={{
                padding: '6px 8px',
                fontSize: 11,
                color: '#333',
                textAlign: 'left',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {t.name}
            </div>
          </button>
        ))}
      </div>
    </div>
  );
};

/* ─── Media Panel (Content Hub Browser) ───────────────────────── */

const MediaPanel = ({
  assets,
  onAddImage,
  onAddVideo,
  onSearch,
  onFilterChange,
  onLoadMore,
  isLoading,
  hasMore,
  activeFilter,
}: {
  assets: MediaAsset[];
  onAddImage: (asset: MediaAsset) => void;
  onAddVideo: (asset: MediaAsset) => void;
  onSearch: (term: string) => void;
  onFilterChange: (type: string) => void;
  onLoadMore: () => void;
  isLoading: boolean;
  hasMore: boolean;
  activeFilter: string;
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const searchTimeout = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleSearchChange = (val: string) => {
    setSearchTerm(val);
    if (searchTimeout.current) clearTimeout(searchTimeout.current);
    searchTimeout.current = setTimeout(() => onSearch(val), 400);
  };

  const images = assets.filter((a) => a.type === 'image');
  const videos = assets.filter((a) => a.type === 'video');
  const filtered =
    activeFilter === 'image'
      ? images
      : activeFilter === 'video'
        ? videos
        : assets;

  return (
    <div style={panelStyles.container}>
      <h3 style={panelStyles.title}>Content Hub Media</h3>

      {/* Search */}
      <div style={{ marginBottom: 10 }}>
        <input
          type="text"
          placeholder="Search images & videos..."
          value={searchTerm}
          onChange={(e) => handleSearchChange(e.target.value)}
          style={{
            width: '100%',
            padding: '7px 10px',
            border: '1px solid #ddd',
            borderRadius: 6,
            fontSize: 12,
            boxSizing: 'border-box',
          }}
        />
      </div>

      {/* Filter tabs */}
      <div
        style={{
          display: 'flex',
          gap: 4,
          marginBottom: 10,
        }}
      >
        {[
          { key: 'all', label: 'All', count: assets.length },
          { key: 'image', label: 'Images', count: images.length },
          { key: 'video', label: 'Videos', count: videos.length },
        ].map((f) => (
          <button
            key={f.key}
            onClick={() => onFilterChange(f.key)}
            style={{
              flex: 1,
              padding: '5px 6px',
              fontSize: 11,
              fontWeight: activeFilter === f.key ? 700 : 400,
              border: '1px solid',
              borderColor: activeFilter === f.key ? '#0078d4' : '#ddd',
              borderRadius: 4,
              background: activeFilter === f.key ? '#e8f0fe' : '#fff',
              color: activeFilter === f.key ? '#0078d4' : '#666',
              cursor: 'pointer',
            }}
          >
            {f.label} ({f.count})
          </button>
        ))}
      </div>

      {/* Loading state */}
      {isLoading && assets.length === 0 && (
        <div
          style={{
            textAlign: 'center',
            padding: 24,
            color: '#999',
            fontSize: 13,
          }}
        >
          <div style={{ fontSize: 24, marginBottom: 8 }}>⏳</div>
          Loading from Content Hub...
        </div>
      )}

      {/* Empty state */}
      {!isLoading && filtered.length === 0 && (
        <div
          style={{
            textAlign: 'center',
            padding: 24,
            color: '#999',
            fontSize: 13,
          }}
        >
          <div style={{ fontSize: 28, marginBottom: 8 }}>📁</div>
          {searchTerm
            ? 'No results found. Try a different search.'
            : 'No media assets in Content Hub yet.'}
        </div>
      )}

      {/* Asset grid */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6 }}>
        {filtered.map((asset) => (
          <button
            key={asset.id}
            onClick={() =>
              asset.type === 'image' ? onAddImage(asset) : onAddVideo(asset)
            }
            style={{
              display: 'flex',
              flexDirection: 'column',
              border: '1px solid #e0e0e0',
              borderRadius: 6,
              overflow: 'hidden',
              background: '#fff',
              cursor: 'pointer',
              padding: 0,
              transition: 'border-color 0.15s, box-shadow 0.15s',
            }}
            title={`${asset.name}\n${asset.type === 'image' ? `${asset.width}×${asset.height}` : 'Video'}`}
          >
            <div
              style={{
                width: '100%',
                height: 80,
                backgroundColor: '#f5f5f5',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                overflow: 'hidden',
                position: 'relative',
              }}
            >
              {asset.type === 'image' ? (
                <img
                  src={asset.url}
                  alt={asset.name}
                  title={asset.name}
                  style={{
                    width: '100%',
                    height: '100%',
                    objectFit: 'cover',
                  }}
                  loading="lazy"
                />
              ) : (
                <div
                  style={{
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    gap: 4,
                  }}
                >
                  <span style={{ fontSize: 24 }}>🎬</span>
                  <span style={{ fontSize: 9, color: '#999' }}>VIDEO</span>
                </div>
              )}
              {/* Type badge */}
              <span
                style={{
                  position: 'absolute',
                  top: 4,
                  right: 4,
                  fontSize: 9,
                  padding: '2px 5px',
                  borderRadius: 3,
                  background:
                    asset.type === 'image'
                      ? 'rgba(52,152,219,0.85)'
                      : 'rgba(231,76,60,0.85)',
                  color: '#fff',
                  fontWeight: 600,
                  textTransform: 'uppercase',
                }}
              >
                {asset.type}
              </span>
            </div>
            <div
              style={{
                padding: '6px 8px',
                fontSize: 11,
                color: '#333',
                textAlign: 'left',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {asset.name}
            </div>
          </button>
        ))}
      </div>

      {/* Load more */}
      {hasMore && !isLoading && (
        <button
          onClick={onLoadMore}
          style={{
            width: '100%',
            padding: '8px',
            marginTop: 10,
            background: '#f5f5f5',
            border: '1px solid #ddd',
            borderRadius: 4,
            fontSize: 12,
            cursor: 'pointer',
            color: '#0078d4',
            fontWeight: 600,
          }}
        >
          Load more…
        </button>
      )}
      {isLoading && assets.length > 0 && (
        <div
          style={{
            textAlign: 'center',
            padding: 8,
            color: '#999',
            fontSize: 12,
          }}
        >
          Loading…
        </div>
      )}
    </div>
  );
};

const TextPanel = ({
  onAddText,
}: {
  onAddText: (preset: Partial<DesignElement>) => void;
}) => {
  const textOptions = [
    {
      icon: <IconText />,
      title: 'Plain Text',
      desc: 'Add simple text',
      preset: {
        text: 'New Text',
        fontSize: 18,
        fontWeight: 'normal',
        fill: '#555555',
      },
    },
    {
      icon: (
        <svg
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#1976d2"
          strokeWidth="1.8"
        >
          <polyline points="4 7 4 4 20 4 20 7" />
          <line x1="12" y1="4" x2="12" y2="20" />
          <path d="M8 20h8" strokeDasharray="2 2" />
        </svg>
      ),
      title: 'Fancy Text',
      desc: 'Add creative font styles',
      preset: {
        text: 'Heading',
        fontSize: 48,
        fontWeight: 'bold',
        fill: '#000000',
      },
    },
    {
      icon: (
        <svg
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#1976d2"
          strokeWidth="1.8"
        >
          <rect x="2" y="6" width="20" height="12" rx="2" />
          <line x1="6" y1="12" x2="18" y2="12" />
          <line x1="6" y1="15" x2="14" y2="15" />
        </svg>
      ),
      title: 'Subtitles',
      desc: 'Add subtitles to your design',
      preset: {
        text: 'Subtitle',
        fontSize: 14,
        fontWeight: 'normal',
        fill: '#ffffff',
      },
    },
    {
      icon: (
        <svg
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#1976d2"
          strokeWidth="1.8"
        >
          <rect x="3" y="3" width="18" height="18" rx="2" />
          <rect x="7" y="7" width="10" height="10" rx="1" />
        </svg>
      ),
      title: 'Slideshow',
      desc: 'Add a text slideshow',
      preset: {
        text: 'Slide Text',
        fontSize: 32,
        fontWeight: '600',
        fill: '#333333',
      },
    },
  ];

  return (
    <div style={panelStyles.container}>
      <h3 style={panelStyles.title}>Text</h3>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        {textOptions.map((opt) => (
          <button
            key={opt.title}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 12,
              padding: '12px 14px',
              border: 'none',
              borderRadius: 8,
              background: '#fff',
              cursor: 'pointer',
              transition: 'background 0.15s',
              textAlign: 'left',
            }}
            onClick={() => onAddText(opt.preset)}
            onMouseEnter={(e) => (e.currentTarget.style.background = '#f5f8ff')}
            onMouseLeave={(e) => (e.currentTarget.style.background = '#fff')}
          >
            <div style={{ color: '#1976d2', flexShrink: 0 }}>{opt.icon}</div>
            <div>
              <div style={{ fontSize: 13, fontWeight: 600, color: '#333' }}>
                {opt.title}
              </div>
              <div style={{ fontSize: 11, color: '#999', marginTop: 2 }}>
                {opt.desc}
              </div>
            </div>
          </button>
        ))}
      </div>
    </div>
  );
};

const ShapesPanel = ({
  onAddShape,
}: {
  onAddShape: (
    type: DesignElement['type'],
    extra?: Partial<DesignElement>,
  ) => void;
}) => (
  <div style={panelStyles.container}>
    <h3 style={panelStyles.title}>Shapes</h3>
    <div style={panelStyles.grid}>
      {[
        {
          label: 'Rectangle',
          icon: '⬜',
          action: () =>
            onAddShape('rect', { width: 200, height: 150, fill: '#3498db' }),
        },
        {
          label: 'Square',
          icon: '🟦',
          action: () =>
            onAddShape('rect', { width: 150, height: 150, fill: '#2ecc71' }),
        },
        {
          label: 'Circle',
          icon: '🔵',
          action: () => onAddShape('circle', { radius: 75, fill: '#e74c3c' }),
        },
        {
          label: 'Triangle',
          icon: '🔺',
          action: () =>
            onAddShape('triangle', { width: 150, height: 130, fill: '#9b59b6' }),
        },
        {
          label: 'Star',
          icon: '⭐',
          action: () =>
            onAddShape('star', { width: 150, height: 150, fill: '#f39c12' }),
        },
        {
          label: 'Arrow',
          icon: '➡️',
          action: () =>
            onAddShape('arrow', { width: 200, height: 80, fill: '#1abc9c' }),
        },
        {
          label: 'Pentagon',
          icon: '⬟',
          action: () =>
            onAddShape('pentagon', { width: 150, height: 150, fill: '#e74c3c' }),
        },
        {
          label: 'Hexagon',
          icon: '⬢',
          action: () =>
            onAddShape('hexagon', { width: 150, height: 150, fill: '#3498db' }),
        },
        {
          label: 'Heart',
          icon: '❤️',
          action: () =>
            onAddShape('heart', { width: 150, height: 150, fill: '#e84393' }),
        },
        {
          label: 'Line',
          icon: '➖',
          action: () =>
            onAddShape('line', { width: 200, height: 2, fill: '#333333' }),
        },
      ].map((s) => (
        <button
          key={s.label}
          onClick={s.action}
          style={panelStyles.shapeBtn}
          title={s.label}
        >
          <span style={{ fontSize: 28 }}>{s.icon}</span>
          <span style={{ fontSize: 11 }}>{s.label}</span>
        </button>
      ))}
    </div>
  </div>
);

const BackgroundPanel = ({
  bgColor,
  onChange,
}: {
  bgColor: string;
  onChange: (c: string) => void;
}) => {
  const [mode, setMode] = useState<'menu' | 'solid' | 'gradient'>('menu');
  const [gradientStart, setGradientStart] = useState('#3498db');
  const [gradientEnd, setGradientEnd] = useState('#e74c3c');
  const [gradientAngle, setGradientAngle] = useState(90);
  
  const presets = [
    '#ffffff',
    '#f5f5f5',
    '#1a1a2e',
    '#2d3436',
    '#0a3d62',
    '#e74c3c',
    '#2ecc71',
    '#3498db',
    '#9b59b6',
    '#f39c12',
    '#1abc9c',
    '#e84393',
    '#6c5ce7',
    '#fdcb6e',
    '#00cec9',
    '#ff6b35',
    '#d63031',
    '#74b9ff',
    '#55efc4',
    '#ffeaa7',
  ];

  if (mode === 'menu') {
    const bgOptions = [
      {
        icon: (
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            stroke="#1976d2"
            strokeWidth="1.8"
          >
            <rect
              x="3"
              y="3"
              width="18"
              height="18"
              rx="2"
              fill="#1976d2"
              fillOpacity="0.15"
            />
          </svg>
        ),
        title: 'Solid',
        desc: 'Add a solid background',
        action: () => setMode('solid'),
      },
      {
        icon: (
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            stroke="#1976d2"
            strokeWidth="1.8"
          >
            <defs>
              <linearGradient id="bg-grad" x1="0" y1="0" x2="1" y2="1">
                <stop offset="0%" stopColor="#42a5f5" />
                <stop offset="100%" stopColor="#7c4dff" />
              </linearGradient>
            </defs>
            <rect
              x="3"
              y="3"
              width="18"
              height="18"
              rx="2"
              fill="url(#bg-grad)"
              fillOpacity="0.4"
            />
          </svg>
        ),
        title: 'Gradient',
        desc: 'Create a gradient background',
        action: () => setMode('gradient'),
      },
      {
        icon: <IconUpload />,
        title: 'My Backgrounds',
        desc: 'Add from your uploads',
        action: () => {},
      },
      {
        icon: <IconMedia />,
        title: 'Stock Photo',
        desc: 'Search and add stock photos',
        action: () => {},
      },
    ];
    return (
      <div style={panelStyles.container}>
        <h3 style={panelStyles.title}>Background</h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {bgOptions.map((opt) => (
            <button
              key={opt.title}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 12,
                padding: '12px 14px',
                border: 'none',
                borderRadius: 8,
                background: '#fff',
                cursor: 'pointer',
                textAlign: 'left',
                transition: 'background 0.15s',
              }}
              onClick={opt.action}
              onMouseEnter={(e) =>
                (e.currentTarget.style.background = '#f5f8ff')
              }
              onMouseLeave={(e) => (e.currentTarget.style.background = '#fff')}
            >
              <div style={{ color: '#1976d2', flexShrink: 0 }}>{opt.icon}</div>
              <div>
                <div style={{ fontSize: 13, fontWeight: 600, color: '#333' }}>
                  {opt.title}
                </div>
                <div style={{ fontSize: 11, color: '#999', marginTop: 2 }}>
                  {opt.desc}
                </div>
              </div>
            </button>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div style={panelStyles.container}>
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          marginBottom: 12,
        }}
      >
        <button
          onClick={() => setMode('menu')}
          style={{
            background: 'none',
            border: 'none',
            cursor: 'pointer',
            fontSize: 16,
            padding: 0,
          }}
        >
          ←
        </button>
        <h3 style={{ ...panelStyles.title, marginBottom: 0 }}>
          {mode === 'solid' ? 'Solid Color' : 'Gradient'}
        </h3>
      </div>
      {mode === 'gradient' ? (
        <>
          <div style={{ marginBottom: 12 }}>
            <label style={panelStyles.label}>Start Color</label>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <input
                type="color"
                value={gradientStart}
                onChange={(e) => setGradientStart(e.target.value)}
                style={{ width: 40, height: 32, border: 'none', cursor: 'pointer' }}
                title="Gradient start color picker"
              />
              <input
                type="text"
                value={gradientStart}
                onChange={(e) => setGradientStart(e.target.value)}
                style={panelStyles.colorInput}
                title="Gradient start color hex"
              />
            </div>
          </div>
          <div style={{ marginBottom: 12 }}>
            <label style={panelStyles.label}>End Color</label>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <input
                type="color"
                value={gradientEnd}
                onChange={(e) => setGradientEnd(e.target.value)}
                style={{ width: 40, height: 32, border: 'none', cursor: 'pointer' }}
                title="Gradient end color picker"
              />
              <input
                type="text"
                value={gradientEnd}
                onChange={(e) => setGradientEnd(e.target.value)}
                style={panelStyles.colorInput}
                title="Gradient end color hex"
              />
            </div>
          </div>
          <div style={{ marginBottom: 12 }}>
            <label style={panelStyles.label}>Angle: {gradientAngle}°</label>
            <input
              type="range"
              min={0}
              max={360}
              value={gradientAngle}
              onChange={(e) => setGradientAngle(+e.target.value)}
              style={{ width: '100%' }}
              title="Gradient angle"
            />
          </div>
          <button
            style={panelStyles.button}
            onClick={() => {
              const gradient = `linear-gradient(${gradientAngle}deg, ${gradientStart}, ${gradientEnd})`;
              onChange(gradient);
            }}
          >
            Apply Gradient
          </button>
        </>
      ) : (
        <>
          <div style={{ marginBottom: 12 }}>
            <label style={panelStyles.label}>Color</label>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <input
                type="color"
                value={bgColor}
                onChange={(e) => onChange(e.target.value)}
                style={{ width: 40, height: 32, border: 'none', cursor: 'pointer' }}
                title="Background color picker"
              />
              <input
                type="text"
                value={bgColor}
                onChange={(e) => onChange(e.target.value)}
                style={panelStyles.colorInput}
                title="Background color hex"
              />
            </div>
          </div>
          <div style={panelStyles.swatchGrid}>
            {presets.map((c) => (
              <button
                key={c}
                onClick={() => onChange(c)}
                style={{
                  ...panelStyles.swatch,
                  backgroundColor: c,
                  border: c === bgColor ? '3px solid #1976d2' : '1px solid #ddd',
                }}
                title={c}
              />
            ))}
          </div>
        </>
      )}
    </div>
  );
};

const LayersPanel = ({
  elements,
  selectedId,
  onSelect,
  onMoveUp,
  onMoveDown,
  onDelete,
  onToggleLock,
}: {
  elements: DesignElement[];
  selectedId: string | null;
  onSelect: (id: string) => void;
  onMoveUp: (id: string) => void;
  onMoveDown: (id: string) => void;
  onDelete: (id: string) => void;
  onToggleLock: (id: string) => void;
}) => (
  <div style={panelStyles.container}>
    <h3 style={panelStyles.title}>Layers</h3>
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      {[...elements].reverse().map((el) => (
        <div
          key={el.id}
          onClick={() => onSelect(el.id)}
          style={{
            ...panelStyles.layerItem,
            backgroundColor: el.id === selectedId ? '#e8f0fe' : '#fff',
            borderColor: el.id === selectedId ? '#0078d4' : '#e0e0e0',
          }}
        >
          <span
            style={{
              fontSize: 12,
              flex: 1,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {el.type === 'text'
              ? `T: ${el.text?.slice(0, 20)}`
              : el.type === 'video'
                ? '🎥 Video'
                : el.type === 'image'
                  ? '🖼️ Image'
                  : el.type}
          </span>
          <div style={{ display: 'flex', gap: 2 }}>
            <button
              onClick={(e) => {
                e.stopPropagation();
                onMoveUp(el.id);
              }}
              style={panelStyles.iconBtn}
              title="Move up"
            >
              ▲
            </button>
            <button
              onClick={(e) => {
                e.stopPropagation();
                onMoveDown(el.id);
              }}
              style={panelStyles.iconBtn}
              title="Move down"
            >
              ▼
            </button>
            <button
              onClick={(e) => {
                e.stopPropagation();
                onToggleLock(el.id);
              }}
              style={panelStyles.iconBtn}
              title="Lock/Unlock"
            >
              {el.locked ? '🔒' : '🔓'}
            </button>
            <button
              onClick={(e) => {
                e.stopPropagation();
                onDelete(el.id);
              }}
              style={{ ...panelStyles.iconBtn, color: '#e74c3c' }}
              title="Delete"
            >
              ✕
            </button>
          </div>
        </div>
      ))}
      {elements.length === 0 && (
        <p
          style={{
            color: '#999',
            fontSize: 13,
            textAlign: 'center',
            padding: 16,
          }}
        >
          No elements yet. Add text or shapes to get started.
        </p>
      )}
    </div>
  </div>
);

/* ─── Properties Panel (right side) ───────────────────────────── */

const PropertiesPanel = ({
  element,
  onChange,
}: {
  element: DesignElement | null;
  onChange: (updates: Partial<DesignElement>) => void;
}) => {
  if (!element)
    return (
      <div style={panelStyles.container}>
        <h3 style={panelStyles.title}>Properties</h3>
        <p style={{ color: '#999', fontSize: 13, padding: 8 }}>
          Select an element to edit its properties.
        </p>
      </div>
    );

  return (
    <div style={panelStyles.container}>
      <h3 style={panelStyles.title}>Properties</h3>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {/* Position */}
        <div style={panelStyles.propGroup}>
          <span style={panelStyles.propLabel}>Position</span>
          <div style={{ display: 'flex', gap: 6 }}>
            <label style={panelStyles.microLabel}>X</label>
            <input
              type="number"
              value={Math.round(element.x)}
              onChange={(e) => onChange({ x: +e.target.value })}
              style={panelStyles.numInput}
              title="X position"
            />
            <label style={panelStyles.microLabel}>Y</label>
            <input
              type="number"
              value={Math.round(element.y)}
              onChange={(e) => onChange({ y: +e.target.value })}
              style={panelStyles.numInput}
              title="Y position"
            />
          </div>
        </div>

        {/* Size */}
        {(element.type === 'rect' ||
          element.type === 'image' ||
          element.type === 'video' ||
          element.type === 'line') && (
          <div style={panelStyles.propGroup}>
            <span style={panelStyles.propLabel}>Size</span>
            <div style={{ display: 'flex', gap: 6 }}>
              <label style={panelStyles.microLabel}>W</label>
              <input
                type="number"
                value={Math.round(element.width ?? 0)}
                onChange={(e) => onChange({ width: +e.target.value })}
                style={panelStyles.numInput}
                title="Width"
              />
              <label style={panelStyles.microLabel}>H</label>
              <input
                type="number"
                value={Math.round(element.height ?? 0)}
                onChange={(e) => onChange({ height: +e.target.value })}
                style={panelStyles.numInput}
                title="Height"
              />
            </div>
          </div>
        )}

        {element.type === 'circle' && (
          <div style={panelStyles.propGroup}>
            <span style={panelStyles.propLabel}>Radius</span>
            <input
              type="number"
              value={element.radius ?? 50}
              onChange={(e) => onChange({ radius: +e.target.value })}
              style={panelStyles.numInput}
              title="Radius"
            />
          </div>
        )}

        {/* Text props */}
        {element.type === 'text' && (
          <>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Text</span>
              <textarea
                value={element.text ?? ''}
                onChange={(e) => onChange({ text: e.target.value })}
                style={panelStyles.textArea}
                rows={3}
                title="Text content"
              />
            </div>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Font Size</span>
              <input
                type="number"
                value={element.fontSize ?? 16}
                onChange={(e) => onChange({ fontSize: +e.target.value })}
                style={panelStyles.numInput}
                title="Font size"
              />
            </div>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Font Weight</span>
              <select
                value={element.fontWeight ?? 'normal'}
                onChange={(e) => onChange({ fontWeight: e.target.value })}
                style={panelStyles.selectInput}
                title="Font weight"
              >
                <option value="normal">Normal</option>
                <option value="bold">Bold</option>
                <option value="600">Semi Bold</option>
                <option value="300">Light</option>
              </select>
            </div>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Font Family</span>
              <select
                value={element.fontFamily ?? 'Inter, sans-serif'}
                onChange={(e) => onChange({ fontFamily: e.target.value })}
                style={panelStyles.selectInput}
                title="Font family"
              >
                <option value="Inter, sans-serif">Inter</option>
                <option value="Arial, sans-serif">Arial</option>
                <option value="Georgia, serif">Georgia</option>
                <option value="'Courier New', monospace">Courier New</option>
                <option value="'Times New Roman', serif">
                  Times New Roman
                </option>
                <option value="Verdana, sans-serif">Verdana</option>
                <option value="Impact, sans-serif">Impact</option>
              </select>
            </div>
          </>
        )}

        {/* Video controls */}
        {element.type === 'video' && (
          <>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Video URL</span>
              <input
                type="text"
                value={element.videoUrl ?? ''}
                onChange={(e) => onChange({ videoUrl: e.target.value })}
                style={{ ...panelStyles.colorInput, width: '100%' }}
                placeholder="https://..."
                title="Video URL"
              />
            </div>
            <div style={panelStyles.propGroup}>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  marginBottom: 8,
                }}
              >
                <span style={{ fontSize: 12, color: '#666' }}>Autoplay</span>
                <input
                  type="checkbox"
                  checked={element.autoplay ?? false}
                  onChange={(e) => onChange({ autoplay: e.target.checked })}
                  title="Autoplay video"
                />
              </div>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  marginBottom: 8,
                }}
              >
                <span style={{ fontSize: 12, color: '#666' }}>Loop</span>
                <input
                  type="checkbox"
                  checked={element.loop ?? false}
                  onChange={(e) => onChange({ loop: e.target.checked })}
                  title="Loop video"
                />
              </div>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                }}
              >
                <span style={{ fontSize: 12, color: '#666' }}>Muted</span>
                <input
                  type="checkbox"
                  checked={element.muted ?? false}
                  onChange={(e) => onChange({ muted: e.target.checked })}
                  title="Mute video"
                />
              </div>
            </div>
          </>
        )}

        {/* Line controls */}
        {element.type === 'line' && (
          <>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Stroke Width</span>
              <input
                type="number"
                value={element.strokeWidth ?? 2}
                onChange={(e) => onChange({ strokeWidth: +e.target.value })}
                style={panelStyles.numInput}
                min={1}
                max={50}
                title="Stroke width"
              />
            </div>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Stroke Style</span>
              <select
                value={element.strokeStyle ?? 'solid'}
                onChange={(e) =>
                  onChange({
                    strokeStyle: e.target.value as 'solid' | 'dashed' | 'dotted',
                  })
                }
                style={panelStyles.selectInput}
                title="Stroke style"
              >
                <option value="solid">Solid</option>
                <option value="dashed">Dashed</option>
                <option value="dotted">Dotted</option>
              </select>
            </div>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Line Cap</span>
              <select
                value={element.lineCap ?? 'butt'}
                onChange={(e) =>
                  onChange({
                    lineCap: e.target.value as 'butt' | 'round' | 'square',
                  })
                }
                style={panelStyles.selectInput}
                title="Line cap"
              >
                <option value="butt">Butt</option>
                <option value="round">Round</option>
                <option value="square">Square</option>
              </select>
            </div>
            <div style={panelStyles.propGroup}>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  marginBottom: 8,
                }}
              >
                <span style={{ fontSize: 12, color: '#666' }}>Start Arrow</span>
                <input
                  type="checkbox"
                  checked={element.arrowStart ?? false}
                  onChange={(e) => onChange({ arrowStart: e.target.checked })}
                  title="Show arrow at start"
                />
              </div>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                }}
              >
                <span style={{ fontSize: 12, color: '#666' }}>End Arrow</span>
                <input
                  type="checkbox"
                  checked={element.arrowEnd ?? false}
                  onChange={(e) => onChange({ arrowEnd: e.target.checked })}
                  title="Show arrow at end"
                />
              </div>
            </div>
          </>
        )}

        {/* Audio controls */}
        {element.type === 'audio' && (
          <>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Audio URL</span>
              <input
                type="text"
                value={element.audioUrl ?? ''}
                onChange={(e) => onChange({ audioUrl: e.target.value })}
                style={{ ...panelStyles.colorInput, width: '100%' }}
                placeholder="https://..."
                title="Audio URL"
              />
            </div>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Volume</span>
              <input
                type="range"
                min={0}
                max={1}
                step={0.05}
                value={element.volume ?? 1}
                onChange={(e) => onChange({ volume: +e.target.value })}
                style={{ width: '100%' }}
                title="Volume"
              />
              <span style={{ fontSize: 11, color: '#666' }}>
                {Math.round((element.volume ?? 1) * 100)}%
              </span>
            </div>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Fade In (seconds)</span>
              <input
                type="number"
                value={element.fadeIn ?? 0}
                onChange={(e) => onChange({ fadeIn: +e.target.value })}
                style={panelStyles.numInput}
                min={0}
                step={0.5}
                title="Fade in duration"
              />
            </div>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Fade Out (seconds)</span>
              <input
                type="number"
                value={element.fadeOut ?? 0}
                onChange={(e) => onChange({ fadeOut: +e.target.value })}
                style={panelStyles.numInput}
                min={0}
                step={0.5}
                title="Fade out duration"
              />
            </div>
            <div style={panelStyles.propGroup}>
              <span style={panelStyles.propLabel}>Start Time (seconds)</span>
              <input
                type="number"
                value={element.startTime ?? 0}
                onChange={(e) => onChange({ startTime: +e.target.value })}
                style={panelStyles.numInput}
                min={0}
                step={0.1}
                title="Start time in timeline"
              />
            </div>
          </>
        )}

        {/* Fill color */}
        <div style={panelStyles.propGroup}>
          <span style={panelStyles.propLabel}>Color</span>
          <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            <input
              type="color"
              value={element.fill}
              onChange={(e) => onChange({ fill: e.target.value })}
              style={{
                width: 32,
                height: 28,
                border: 'none',
                cursor: 'pointer',
              }}
              title="Fill color picker"
            />
            <input
              type="text"
              value={element.fill}
              onChange={(e) => onChange({ fill: e.target.value })}
              style={{ ...panelStyles.colorInput, flex: 1 }}
              title="Fill color hex"
            />
          </div>
        </div>

        {/* Opacity */}
        <div style={panelStyles.propGroup}>
          <span style={panelStyles.propLabel}>Opacity</span>
          <input
            type="range"
            min={0}
            max={1}
            step={0.05}
            value={element.opacity ?? 1}
            onChange={(e) => onChange({ opacity: +e.target.value })}
            style={{ width: '100%' }}
            title="Opacity"
          />
          <span style={{ fontSize: 11, color: '#666' }}>
            {Math.round((element.opacity ?? 1) * 100)}%
          </span>
        </div>

        {/* Rotation */}
        <div style={panelStyles.propGroup}>
          <span style={panelStyles.propLabel}>Rotation</span>
          <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            <input
              type="range"
              min={0}
              max={360}
              value={element.rotation ?? 0}
              onChange={(e) => onChange({ rotation: +e.target.value })}
              style={{ flex: 1 }}
              title="Rotation"
            />
            <span style={{ fontSize: 11, color: '#666', width: 30 }}>
              {element.rotation ?? 0}°
            </span>
          </div>
        </div>

        {/* Shadow */}
        <div style={panelStyles.propGroup}>
          <span style={panelStyles.propLabel}>Shadow</span>
          <div style={{ marginBottom: 8 }}>
            <label style={{ fontSize: 11, color: '#666' }}>Blur</label>
            <input
              type="number"
              value={element.shadowBlur ?? 0}
              onChange={(e) => onChange({ shadowBlur: +e.target.value })}
              style={panelStyles.numInput}
              min={0}
              max={50}
              title="Shadow blur"
            />
          </div>
          <div style={{ marginBottom: 8 }}>
            <label style={{ fontSize: 11, color: '#666' }}>Color</label>
            <input
              type="color"
              value={element.shadowColor ?? '#000000'}
              onChange={(e) => onChange({ shadowColor: e.target.value })}
              style={{ width: '100%', height: 28, border: 'none', cursor: 'pointer' }}
              title="Shadow color"
            />
          </div>
          <div style={{ display: 'flex', gap: 6 }}>
            <div style={{ flex: 1 }}>
              <label style={{ fontSize: 11, color: '#666' }}>Offset X</label>
              <input
                type="number"
                value={element.shadowOffsetX ?? 0}
                onChange={(e) => onChange({ shadowOffsetX: +e.target.value })}
                style={panelStyles.numInput}
                min={-50}
                max={50}
                title="Shadow offset X"
              />
            </div>
            <div style={{ flex: 1 }}>
              <label style={{ fontSize: 11, color: '#666' }}>Offset Y</label>
              <input
                type="number"
                value={element.shadowOffsetY ?? 0}
                onChange={(e) => onChange({ shadowOffsetY: +e.target.value })}
                style={panelStyles.numInput}
                min={-50}
                max={50}
                title="Shadow offset Y"
              />
            </div>
          </div>
        </div>

        {/* Duplicate button */}
        <button
          style={{
            ...panelStyles.button,
            marginTop: 8,
          }}
          onClick={() => {
            // Trigger duplicate via custom event or callback
            const event = new CustomEvent('duplicateElement', { detail: { id: element.id } });
            window.dispatchEvent(event);
          }}
        >
          Duplicate Element
        </button>
      </div>
    </div>
  );
};

/* ─── Canvas Renderer (SVG-based) ─────────────────────────────── */

const CanvasRenderer = ({
  design,
  selectedId,
  onSelectElement,
  onMoveElement,
  canvasScale,
}: {
  design: DesignState;
  selectedId: string | null;
  onSelectElement: (id: string | null) => void;
  onMoveElement: (id: string, dx: number, dy: number) => void;
  canvasScale: number;
}) => {
  const svgRef = useRef<SVGSVGElement>(null);
  const [dragging, setDragging] = useState<{
    id: string;
    startX: number;
    startY: number;
    elX: number;
    elY: number;
  } | null>(null);

  const handleMouseDown = useCallback(
    (e: React.MouseEvent, el: DesignElement) => {
      if (el.locked) return;
      e.stopPropagation();
      onSelectElement(el.id);
      setDragging({
        id: el.id,
        startX: e.clientX,
        startY: e.clientY,
        elX: el.x,
        elY: el.y,
      });
    },
    [onSelectElement],
  );

  const handleMouseMove = useCallback(
    (e: React.MouseEvent) => {
      if (!dragging) return;
      const dx = (e.clientX - dragging.startX) / canvasScale;
      const dy = (e.clientY - dragging.startY) / canvasScale;
      onMoveElement(dragging.id, dragging.elX + dx, dragging.elY + dy);
    },
    [dragging, canvasScale, onMoveElement],
  );

  const handleMouseUp = useCallback(() => {
    setDragging(null);
  }, []);

  const renderElement = (el: DesignElement) => {
    const isSelected = el.id === selectedId;
    const commonProps = {
      key: el.id,
      onMouseDown: (e: React.MouseEvent) => handleMouseDown(e, el),
      style: {
        cursor: el.locked ? 'not-allowed' : 'move',
      } as React.CSSProperties,
    };

    const transform = el.rotation
      ? `rotate(${el.rotation} ${el.x + (el.width ?? 0) / 2} ${el.y + (el.height ?? 0) / 2})`
      : undefined;

    switch (el.type) {
      case 'text':
        return (
          <g {...commonProps} transform={transform}>
            <text
              x={el.x}
              y={el.y}
              fill={el.fill}
              fontSize={el.fontSize ?? 16}
              fontWeight={el.fontWeight ?? 'normal'}
              fontFamily={el.fontFamily ?? 'Inter, sans-serif'}
              opacity={el.opacity ?? 1}
              letterSpacing={el.letterSpacing}
              dominantBaseline="hanging"
            >
              {el.text}
            </text>
            {isSelected && (
              <rect
                x={el.x - 4}
                y={el.y - 4}
                width={(el.text?.length ?? 1) * (el.fontSize ?? 16) * 0.6 + 8}
                height={(el.fontSize ?? 16) + 8}
                fill="none"
                stroke="#0078d4"
                strokeWidth={2}
                strokeDasharray="5,3"
              />
            )}
          </g>
        );
      case 'rect':
        return (
          <g {...commonProps} transform={transform}>
            <rect
              x={el.x}
              y={el.y}
              width={el.width ?? 100}
              height={el.height ?? 100}
              fill={el.fill}
              stroke={el.stroke}
              strokeWidth={el.strokeWidth}
              opacity={el.opacity ?? 1}
              rx={2}
            />
            {isSelected && (
              <rect
                x={el.x - 3}
                y={el.y - 3}
                width={(el.width ?? 100) + 6}
                height={(el.height ?? 100) + 6}
                fill="none"
                stroke="#0078d4"
                strokeWidth={2}
                strokeDasharray="5,3"
              />
            )}
          </g>
        );
      case 'circle':
        return (
          <g {...commonProps}>
            <circle
              cx={el.x}
              cy={el.y}
              r={el.radius ?? 50}
              fill={el.fill}
              stroke={el.stroke}
              strokeWidth={el.strokeWidth}
              opacity={el.opacity ?? 1}
            />
            {isSelected && (
              <circle
                cx={el.x}
                cy={el.y}
                r={(el.radius ?? 50) + 4}
                fill="none"
                stroke="#0078d4"
                strokeWidth={2}
                strokeDasharray="5,3"
              />
            )}
          </g>
        );
      case 'line':
        const strokeStyle = el.strokeStyle ?? 'solid';
        const strokeDasharray =
          strokeStyle === 'dashed'
            ? '10,5'
            : strokeStyle === 'dotted'
              ? '2,3'
              : 'none';
        const lineCap = el.lineCap ?? 'butt';
        const strokeWidth = el.strokeWidth ?? 2;
        const x2 = el.x + (el.width ?? 200);
        const y2 = el.y + (el.height ?? 0);
        const arrowSize = strokeWidth * 4;

        return (
          <g {...commonProps} transform={transform}>
            {/* SVG arrows in defs */}
            {(el.arrowStart || el.arrowEnd) && (
              <defs>
                {el.arrowStart && (
                  <marker
                    id={`arrow-start-${el.id}`}
                    markerWidth={arrowSize}
                    markerHeight={arrowSize}
                    refX={arrowSize / 2}
                    refY={arrowSize / 2}
                    orient="auto"
                    markerUnits="strokeWidth"
                  >
                    <path
                      d={`M ${arrowSize} ${arrowSize / 2} L 0 0 L 0 ${arrowSize} Z`}
                      fill={el.fill}
                    />
                  </marker>
                )}
                {el.arrowEnd && (
                  <marker
                    id={`arrow-end-${el.id}`}
                    markerWidth={arrowSize}
                    markerHeight={arrowSize}
                    refX={arrowSize / 2}
                    refY={arrowSize / 2}
                    orient="auto"
                    markerUnits="strokeWidth"
                  >
                    <path
                      d={`M 0 ${arrowSize / 2} L ${arrowSize} 0 L ${arrowSize} ${arrowSize} Z`}
                      fill={el.fill}
                    />
                  </marker>
                )}
              </defs>
            )}
            <line
              x1={el.x}
              y1={el.y}
              x2={x2}
              y2={y2}
              stroke={el.fill}
              strokeWidth={strokeWidth}
              strokeDasharray={strokeDasharray}
              strokeLinecap={lineCap}
              opacity={el.opacity ?? 1}
              markerStart={el.arrowStart ? `url(#arrow-start-${el.id})` : undefined}
              markerEnd={el.arrowEnd ? `url(#arrow-end-${el.id})` : undefined}
            />
            {isSelected && (
              <line
                x1={el.x}
                y1={el.y}
                x2={x2}
                y2={y2}
                stroke="#0078d4"
                strokeWidth={Math.max(strokeWidth + 6, 8)}
                opacity={0.3}
              />
            )}
          </g>
        );
      case 'image':
        return (
          <g {...commonProps} transform={transform}>
            <image
              href={el.src}
              x={el.x}
              y={el.y}
              width={el.width ?? 200}
              height={el.height ?? 200}
              opacity={el.opacity ?? 1}
              aria-label={el.text ?? 'Image element'}
            >
              <title>{el.text ?? 'Image element'}</title>
            </image>
            {isSelected && (
              <rect
                x={el.x - 3}
                y={el.y - 3}
                width={(el.width ?? 200) + 6}
                height={(el.height ?? 200) + 6}
                fill="none"
                stroke="#0078d4"
                strokeWidth={2}
                strokeDasharray="5,3"
              />
            )}
          </g>
        );
      case 'video':
        return (
          <g {...commonProps}>
            <foreignObject
              x={el.x}
              y={el.y}
              width={el.width ?? 400}
              height={el.height ?? 300}
              opacity={el.opacity ?? 1}
              transform={transform}
            >
              <div
                style={{
                  width: '100%',
                  height: '100%',
                  background: '#000',
                  borderRadius: 4,
                  overflow: 'hidden',
                }}
              >
                <video
                  src={el.videoUrl}
                  autoPlay={el.autoplay ?? false}
                  loop={el.loop ?? true}
                  muted={el.muted ?? true}
                  controls
                  style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                />
              </div>
            </foreignObject>
            {isSelected && (
              <rect
                x={el.x - 3}
                y={el.y - 3}
                width={(el.width ?? 400) + 6}
                height={(el.height ?? 300) + 6}
                fill="none"
                stroke="#0078d4"
                strokeWidth={2}
                strokeDasharray="5,3"
              />
            )}
          </g>
        );
      case 'audio':
        return (
          <g {...commonProps} transform={transform}>
            <rect
              x={el.x}
              y={el.y}
              width={el.width ?? 300}
              height={el.height ?? 40}
              fill={el.fill ?? '#0078d4'}
              opacity={el.opacity ?? 1}
              rx={4}
            />
            {/* Audio icon */}
            <g transform={`translate(${el.x + 8}, ${el.y + 8})`}>
              <path
                d="M9 18V5l12-2v13"
                fill="none"
                stroke="white"
                strokeWidth={1.5}
              />
              <circle cx="6" cy="18" r="3" fill="white" />
              <circle cx="18" cy="16" r="3" fill="white" />
            </g>
            {/* Audio label */}
            <text
              x={el.x + 36}
              y={el.y + (el.height ?? 40) / 2 + 1}
              fill="white"
              fontSize={12}
              fontFamily="Inter, sans-serif"
              dominantBaseline="middle"
            >
              {el.audioUrl?.split('/').pop()?.substring(0, 20) ?? 'Audio Track'}
            </text>
            {isSelected && (
              <rect
                x={el.x - 3}
                y={el.y - 3}
                width={(el.width ?? 300) + 6}
                height={(el.height ?? 40) + 6}
                fill="none"
                stroke="#0078d4"
                strokeWidth={2}
                strokeDasharray="5,3"
              />
            )}
          </g>
        );
      case 'triangle':
        const triW = el.width ?? 150;
        const triH = el.height ?? 130;
        const triPath = `M ${el.x + triW / 2} ${el.y} L ${el.x + triW} ${el.y + triH} L ${el.x} ${el.y + triH} Z`;
        return (
          <g {...commonProps} transform={transform}>
            <path
              d={triPath}
              fill={el.fill}
              stroke={el.stroke}
              strokeWidth={el.strokeWidth}
              opacity={el.opacity ?? 1}
            />
            {isSelected && (
              <rect
                x={el.x - 3}
                y={el.y - 3}
                width={triW + 6}
                height={triH + 6}
                fill="none"
                stroke="#0078d4"
                strokeWidth={2}
                strokeDasharray="5,3"
              />
            )}
          </g>
        );
      case 'star':
        const starW = el.width ?? 150;
        const starH = el.height ?? 150;
        const starCX = el.x + starW / 2;
        const starCY = el.y + starH / 2;
        const outerR = starW / 2;
        const innerR = outerR * 0.4;
        let starPath = '';
        for (let i = 0; i < 10; i++) {
          const angle = (i * Math.PI) / 5 - Math.PI / 2;
          const r = i % 2 === 0 ? outerR : innerR;
          const x = starCX + r * Math.cos(angle);
          const y = starCY + r * Math.sin(angle);
          starPath += `${i === 0 ? 'M' : 'L'} ${x} ${y} `;
        }
        starPath += 'Z';
        return (
          <g {...commonProps} transform={transform}>
            <path
              d={starPath}
              fill={el.fill}
              stroke={el.stroke}
              strokeWidth={el.strokeWidth}
              opacity={el.opacity ?? 1}
            />
            {isSelected && (
              <rect
                x={el.x - 3}
                y={el.y - 3}
                width={starW + 6}
                height={starH + 6}
                fill="none"
                stroke="#0078d4"
                strokeWidth={2}
                strokeDasharray="5,3"
              />
            )}
          </g>
        );
      case 'arrow':
        const arrowW = el.width ?? 200;
        const arrowH = el.height ?? 80;
        const arrowPath = `M ${el.x} ${el.y + arrowH / 2} L ${el.x + arrowW * 0.7} ${el.y + arrowH / 2} L ${el.x + arrowW * 0.7} ${el.y} L ${el.x + arrowW} ${el.y + arrowH / 2} L ${el.x + arrowW * 0.7} ${el.y + arrowH} L ${el.x + arrowW * 0.7} ${el.y + arrowH / 2} Z`;
        return (
          <g {...commonProps} transform={transform}>
            <path
              d={arrowPath}
              fill={el.fill}
              stroke={el.stroke}
              strokeWidth={el.strokeWidth}
              opacity={el.opacity ?? 1}
            />
            {isSelected && (
              <rect
                x={el.x - 3}
                y={el.y - 3}
                width={arrowW + 6}
                height={arrowH + 6}
                fill="none"
                stroke="#0078d4"
                strokeWidth={2}
                strokeDasharray="5,3"
              />
            )}
          </g>
        );
      case 'pentagon':
        const pentW = el.width ?? 150;
        const pentH = el.height ?? 150;
        const pentCX = el.x + pentW / 2;
        const pentCY = el.y + pentH / 2;
        const pentR = pentW / 2;
        let pentPath = '';
        for (let i = 0; i < 5; i++) {
          const angle = ((i * 2 * Math.PI) / 5) - Math.PI / 2;
          const x = pentCX + pentR * Math.cos(angle);
          const y = pentCY + pentR * Math.sin(angle);
          pentPath += `${i === 0 ? 'M' : 'L'} ${x} ${y} `;
        }
        pentPath += 'Z';
        return (
          <g {...commonProps} transform={transform}>
            <path
              d={pentPath}
              fill={el.fill}
              stroke={el.stroke}
              strokeWidth={el.strokeWidth}
              opacity={el.opacity ?? 1}
            />
            {isSelected && (
              <rect
                x={el.x - 3}
                y={el.y - 3}
                width={pentW + 6}
                height={pentH + 6}
                fill="none"
                stroke="#0078d4"
                strokeWidth={2}
                strokeDasharray="5,3"
              />
            )}
          </g>
        );
      case 'hexagon':
        const hexW = el.width ?? 150;
        const hexH = el.height ?? 150;
        const hexCX = el.x + hexW / 2;
        const hexCY = el.y + hexH / 2;
        const hexR = hexW / 2;
        let hexPath = '';
        for (let i = 0; i < 6; i++) {
          const angle = (i * Math.PI) / 3;
          const x = hexCX + hexR * Math.cos(angle);
          const y = hexCY + hexR * Math.sin(angle);
          hexPath += `${i === 0 ? 'M' : 'L'} ${x} ${y} `;
        }
        hexPath += 'Z';
        return (
          <g {...commonProps} transform={transform}>
            <path
              d={hexPath}
              fill={el.fill}
              stroke={el.stroke}
              strokeWidth={el.strokeWidth}
              opacity={el.opacity ?? 1}
            />
            {isSelected && (
              <rect
                x={el.x - 3}
                y={el.y - 3}
                width={hexW + 6}
                height={hexH + 6}
                fill="none"
                stroke="#0078d4"
                strokeWidth={2}
                strokeDasharray="5,3"
              />
            )}
          </g>
        );
      case 'heart':
        const heartW = el.width ?? 150;
        const heartH = el.height ?? 150;
        const heartScale = heartW / 150;
        const heartPath = `M ${el.x + 75 * heartScale} ${el.y + 40 * heartScale} 
          C ${el.x + 75 * heartScale} ${el.y + 30 * heartScale}, 
            ${el.x + 50 * heartScale} ${el.y + 10 * heartScale}, 
            ${el.x + 30 * heartScale} ${el.y + 10 * heartScale} 
          C ${el.x + 10 * heartScale} ${el.y + 10 * heartScale}, 
            ${el.x} ${el.y + 30 * heartScale}, 
            ${el.x} ${el.y + 50 * heartScale} 
          C ${el.x} ${el.y + 80 * heartScale}, 
            ${el.x + 30 * heartScale} ${el.y + 110 * heartScale}, 
            ${el.x + 75 * heartScale} ${el.y + 150 * heartScale} 
          C ${el.x + 120 * heartScale} ${el.y + 110 * heartScale}, 
            ${el.x + 150 * heartScale} ${el.y + 80 * heartScale}, 
            ${el.x + 150 * heartScale} ${el.y + 50 * heartScale} 
          C ${el.x + 150 * heartScale} ${el.y + 30 * heartScale}, 
            ${el.x + 140 * heartScale} ${el.y + 10 * heartScale}, 
            ${el.x + 120 * heartScale} ${el.y + 10 * heartScale} 
          C ${el.x + 100 * heartScale} ${el.y + 10 * heartScale}, 
            ${el.x + 75 * heartScale} ${el.y + 30 * heartScale}, 
            ${el.x + 75 * heartScale} ${el.y + 40 * heartScale} Z`;
        return (
          <g {...commonProps} transform={transform}>
            <path
              d={heartPath}
              fill={el.fill}
              stroke={el.stroke}
              strokeWidth={el.strokeWidth}
              opacity={el.opacity ?? 1}
            />
            {isSelected && (
              <rect
                x={el.x - 3}
                y={el.y - 3}
                width={heartW + 6}
                height={heartH + 6}
                fill="none"
                stroke="#0078d4"
                strokeWidth={2}
                strokeDasharray="5,3"
              />
            )}
          </g>
        );
      default:
        return null;
    }
  };

  return (
    <svg
      ref={svgRef}
      viewBox={`0 0 ${design.width} ${design.height}`}
      width={design.width * canvasScale}
      height={design.height * canvasScale}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseLeave={handleMouseUp}
      onClick={(e) => {
        if (e.target === svgRef.current) onSelectElement(null);
      }}
      style={{
        backgroundColor: design.backgroundColor,
        boxShadow: '0 4px 24px rgba(0,0,0,0.15)',
        borderRadius: 4,
      }}
    >
      {/* Grid pattern */}
      <defs>
        <pattern id="grid" width="50" height="50" patternUnits="userSpaceOnUse">
          <path
            d="M 50 0 L 0 0 0 50"
            fill="none"
            stroke="rgba(0,0,0,0.03)"
            strokeWidth="1"
          />
        </pattern>
      </defs>
      <rect width="100%" height="100%" fill="url(#grid)" />

      {design.elements.map(renderElement)}
    </svg>
  );
};

/* ─── Main Component ──────────────────────────────────────────── */

export const DesignBuilderLayoutTemplate = (
  props: DesignBuilderClientProperties,
) => {
  // State
  const [design, setDesign] = useState<DesignState>(() => {
    if (props.currentDesignJson) {
      try {
        return JSON.parse(props.currentDesignJson) as DesignState;
      } catch {
        return defaultDesign();
      }
    }
    return defaultDesign();
  });

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<SidebarTab>('templates');
  const [designName, setDesignName] = useState('Untitled Design');

  // History for undo/redo
  const [history, setHistory] = useState<DesignState[]>([]);
  const [historyIndex, setHistoryIndex] = useState(-1);

  // Modal states
  const [showFileMenu, setShowFileMenu] = useState(false);
  const [showShareModal, setShowShareModal] = useState(false);
  const [showHelpModal, setShowHelpModal] = useState(false);
  const [showPublishModal, setShowPublishModal] = useState(false);
  const [showTimelineModal, setShowTimelineModal] = useState(false);
  const [showMusicModal, setShowMusicModal] = useState(false);
  const [showAnimationModal, setShowAnimationModal] = useState(false);

  // Layout toggle states
  const [showGrid, setShowGrid] = useState(false);
  const [showBleed, setShowBleed] = useState(false);
  const [showAlignmentGuides, setShowAlignmentGuides] = useState(true);
  const [canvasScale, setCanvasScale] = useState(0.5);
  const [showSizeModal, setShowSizeModal] = useState(false);
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);

  // Combine built-in templates with props templates
  const allTemplates = React.useMemo(() => {
    const builtIn = createBuiltInTemplates();
    return [...builtIn, ...props.templates];
  }, [props.templates]);

  // Commands
  const { execute: saveDesign } = usePageCommand<
    { designId: number; savedAt: string },
    { name: string; designJson: string; width: number; height: number }
  >('SAVE_DESIGN', {
    after(res) {
      if (res) {
        setHasUnsavedChanges(false);
      }
    },
  });

  const { execute: exportDesign } = usePageCommand<
    { downloadUrl: string },
    { designId: number; format: string }
  >('EXPORT_DESIGN');

  // ── Content Hub Media State ──
  const [hubAssets, setHubAssets] = useState<MediaAsset[]>([]);
  const [hubLoading, setHubLoading] = useState(false);
  const [hubHasMore, setHubHasMore] = useState(false);
  const [hubFilter, setHubFilter] = useState('all');
  const [hubSearchTerm, setHubSearchTerm] = useState('');
  const [hubOffset, setHubOffset] = useState(0);
  const hubPageSize = 50;

  const { execute: browseContentHub } = usePageCommand<
    { assets: MediaAsset[]; totalCount: number; hasMore: boolean },
    {
      searchTerm: string | null;
      mediaType: string | null;
      offset: number;
      limit: number;
    }
  >('BROWSE_CONTENT_HUB');

  const loadHubAssets = useCallback(
    async (search: string, filter: string, offset: number, append: boolean) => {
      setHubLoading(true);
      try {
        const res = (await browseContentHub({
          searchTerm: search || null,
          mediaType: filter === 'all' ? null : filter,
          offset,
          limit: hubPageSize,
        })) as { assets: MediaAsset[]; hasMore: boolean } | undefined;
        if (res) {
          setHubAssets((prev) =>
            append ? [...prev, ...res.assets] : res.assets,
          );
          setHubHasMore(res.hasMore);
          setHubOffset(offset + hubPageSize);
        }
      } finally {
        setHubLoading(false);
      }
    },
    [browseContentHub],
  );

  // Load hub assets when media tab is first opened
  useEffect(() => {
    if (activeTab === 'media' && hubAssets.length === 0 && !hubLoading) {
      void loadHubAssets('', 'all', 0, false);
    }
  }, [activeTab, hubAssets.length, hubLoading, loadHubAssets]);

  const handleHubSearch = useCallback(
    (term: string) => {
      setHubSearchTerm(term);
      setHubOffset(0);
      void loadHubAssets(term, hubFilter, 0, false);
    },
    [hubFilter, loadHubAssets],
  );

  const handleHubFilterChange = useCallback((type: string) => {
    setHubFilter(type);
    // no need to re-fetch, filtering is client-side on already loaded data
  }, []);

  const handleHubLoadMore = useCallback(() => {
    void loadHubAssets(hubSearchTerm, hubFilter, hubOffset, true);
  }, [hubSearchTerm, hubFilter, hubOffset, loadHubAssets]);

  // Handlers
  const markChanged = useCallback(() => setHasUnsavedChanges(true), []);

  const pushHistory = useCallback(
    (newDesign: DesignState) => {
      setHistory((prev) => {
        const newHistory = prev.slice(0, historyIndex + 1);
        return [...newHistory, newDesign];
      });
      setHistoryIndex((i) => i + 1);
    },
    [historyIndex],
  );

  const updateDesign = useCallback(
    (updater: (prev: DesignState) => DesignState) => {
      setDesign((prev) => {
        const next = updater(prev);
        pushHistory(next);
        markChanged();
        return next;
      });
    },
    [markChanged, pushHistory],
  );

  const handleUndo = useCallback(() => {
    if (historyIndex > 0) {
      setHistoryIndex(historyIndex - 1);
      setDesign(history[historyIndex - 1]);
      markChanged();
    }
  }, [historyIndex, history, markChanged]);

  const handleRedo = useCallback(() => {
    if (historyIndex < history.length - 1) {
      setHistoryIndex(historyIndex + 1);
      setDesign(history[historyIndex + 1]);
      markChanged();
    }
  }, [historyIndex, history, markChanged]);

  const handleBack = useCallback(() => {
    if (hasUnsavedChanges) {
      if (window.confirm('You have unsaved changes. Discard them?')) {
        window.history.back();
      }
    } else {
      window.history.back();
    }
  }, [hasUnsavedChanges]);

  const handleNewDesign = useCallback(() => {
    if (hasUnsavedChanges) {
      if (window.confirm('You have unsaved changes. Start a new design?')) {
        setDesign(defaultDesign());
        setDesignName('Untitled Design');
        setHasUnsavedChanges(false);
        setShowFileMenu(false);
      }
    } else {
      setDesign(defaultDesign());
      setDesignName('Untitled Design');
      setShowFileMenu(false);
    }
  }, [hasUnsavedChanges]);

  const handleUploadFiles = useCallback(() => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/*,video/*';
    input.multiple = true;
    input.onchange = (e) => {
      const files = (e.target as HTMLInputElement).files;
      if (files) {
        // In real implementation, upload to server and get URLs
        alert(
          `Selected ${files.length} file(s). Upload functionality would process these files.`,
        );
        // Example: Array.from(files).forEach(file => uploadFile(file));
      }
    };
    input.click();
  }, []);

  const selectedElement =
    design.elements.find((e) => e.id === selectedId) ?? null;

  const addElement = useCallback(
    (el: Omit<DesignElement, 'id'>) => {
      const newEl: DesignElement = { ...el, id: uid() };
      updateDesign((d) => ({ ...d, elements: [...d.elements, newEl] }));
      setSelectedId(newEl.id);
    },
    [updateDesign],
  );

  const updateElement = useCallback(
    (id: string, updates: Partial<DesignElement>) => {
      updateDesign((d) => ({
        ...d,
        elements: d.elements.map((e) =>
          e.id === id ? { ...e, ...updates } : e,
        ),
      }));
    },
    [updateDesign],
  );

  const deleteElement = useCallback(
    (id: string) => {
      updateDesign((d) => ({
        ...d,
        elements: d.elements.filter((e) => e.id !== id),
      }));
      if (selectedId === id) setSelectedId(null);
    },
    [updateDesign, selectedId],
  );

  const moveLayerUp = useCallback(
    (id: string) => {
      updateDesign((d) => {
        const idx = d.elements.findIndex((e) => e.id === id);
        if (idx < d.elements.length - 1) {
          const arr = [...d.elements];
          [arr[idx], arr[idx + 1]] = [arr[idx + 1], arr[idx]];
          return { ...d, elements: arr };
        }
        return d;
      });
    },
    [updateDesign],
  );

  const moveLayerDown = useCallback(
    (id: string) => {
      updateDesign((d) => {
        const idx = d.elements.findIndex((e) => e.id === id);
        if (idx > 0) {
          const arr = [...d.elements];
          [arr[idx], arr[idx - 1]] = [arr[idx - 1], arr[idx]];
          return { ...d, elements: arr };
        }
        return d;
      });
    },
    [updateDesign],
  );

  const toggleLock = useCallback(
    (id: string) => {
      updateElement(id, {
        locked: !design.elements.find((e) => e.id === id)?.locked,
      });
    },
    [updateElement, design.elements],
  );

  const handleAddImageFromHub = useCallback(
    (asset: MediaAsset) => {
      const imgW = asset.width || 300;
      const imgH = asset.height || 300;
      // Scale down if image is larger than canvas
      const scale = Math.min(
        1,
        (design.width * 0.6) / imgW,
        (design.height * 0.6) / imgH,
      );
      addElement({
        type: 'image',
        x: (design.width - imgW * scale) / 2,
        y: (design.height - imgH * scale) / 2,
        width: Math.round(imgW * scale),
        height: Math.round(imgH * scale),
        src: asset.url,
        fill: 'transparent',
        opacity: 1,
      });
    },
    [addElement, design.width, design.height],
  );

  const handleAddVideoFromHub = useCallback(
    (asset: MediaAsset) => {
      // Add video as a proper video element
      addElement({
        type: 'video',
        x: design.width / 2 - 200,
        y: design.height / 2 - 150,
        width: 400,
        height: 300,
        videoUrl: asset.url,
        fill: 'transparent',
        opacity: 1,
        autoplay: false,
        loop: true,
        muted: true,
      });
    },
    [addElement, design.width, design.height],
  );

  const handleSave = useCallback(async () => {
    await saveDesign({
      name: designName,
      designJson: JSON.stringify(design),
      width: design.width,
      height: design.height,
    });
  }, [saveDesign, designName, design]);

  const handleExport = useCallback(
    async (format: string) => {
      await exportDesign({ designId: props.currentDesignId, format });
    },
    [exportDesign, props.currentDesignId],
  );

  const applyTemplate = useCallback(
    (tpl: DesignTemplate) => {
      if (!tpl.designJson || tpl.designJson === '{}') {
        setDesign(defaultDesign(design.width, design.height));
      } else {
        try {
          const parsed = JSON.parse(tpl.designJson) as Partial<DesignState>;
          setDesign({
            backgroundColor: (parsed.backgroundColor as string) ?? '#ffffff',
            width: design.width,
            height: design.height,
            elements: ((parsed.elements as Partial<DesignElement>[]) ?? []).map(
              (e) =>
                ({
                  ...e,
                  id: uid(),
                  type: e.type ?? 'rect',
                  x: e.x ?? 0,
                  y: e.y ?? 0,
                  fill: e.fill ?? '#000000',
                }) as DesignElement,
            ),
          });
        } catch {
          setDesign(defaultDesign(design.width, design.height));
        }
      }
      markChanged();
    },
    [design.width, design.height, markChanged],
  );

  const handleAddText = useCallback(
    (preset: Partial<DesignElement>) => {
      addElement({
        type: 'text',
        x: design.width / 2 - 100,
        y: design.height / 2,
        fill: preset.fill ?? '#000000',
        text: preset.text ?? 'New Text',
        fontSize: preset.fontSize ?? 24,
        fontWeight: preset.fontWeight ?? 'normal',
        fontFamily: 'Inter, sans-serif',
        opacity: 1,
      });
    },
    [addElement, design.width, design.height],
  );

  const handleAddShape = useCallback(
    (type: DesignElement['type'], extra?: Partial<DesignElement>) => {
      addElement({
        type,
        x: design.width / 2 - (extra?.width ?? extra?.radius ?? 50),
        y: design.height / 2 - (extra?.height ?? extra?.radius ?? 50),
        fill: extra?.fill ?? '#cccccc',
        width: extra?.width,
        height: extra?.height,
        radius: extra?.radius,
        opacity: 1,
      });
    },
    [addElement, design.width, design.height],
  );

  const handleAddVideoByUrl = useCallback(
    (url: string) => {
      if (!url) return;
      addElement({
        type: 'video',
        x: design.width / 2 - 200,
        y: design.height / 2 - 150,
        width: 400,
        height: 300,
        videoUrl: url,
        fill: 'transparent',
        opacity: 1,
        autoplay: false,
        loop: true,
        muted: true,
      });
    },
    [addElement, design.width, design.height],
  );

  const handleMoveElement = useCallback(
    (id: string, newX: number, newY: number) => {
      updateElement(id, { x: newX, y: newY });
    },
    [updateElement],
  );

  const handleResize = useCallback(
    (preset: CanvasSizePreset) => {
      setDesign((d) => ({ ...d, width: preset.width, height: preset.height }));
      setShowSizeModal(false);
      markChanged();
    },
    [markChanged],
  );

  // Keyboard shortcuts
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Delete' && selectedId) {
        deleteElement(selectedId);
      }
      if (e.key === 'Escape') {
        setSelectedId(null);
      }
      if (e.ctrlKey && e.key === 's') {
        e.preventDefault();
        void handleSave();
      }
      // Undo with Ctrl+Z
      if (e.ctrlKey && e.key === 'z' && !e.shiftKey) {
        e.preventDefault();
        handleUndo();
      }
      // Redo with Ctrl+Shift+Z or Ctrl+Y
      if (
        (e.ctrlKey && e.shiftKey && e.key === 'z') ||
        (e.ctrlKey && e.key === 'y')
      ) {
        e.preventDefault();
        handleRedo();
      }
      // Duplicate with Ctrl+D
      if (e.ctrlKey && e.key === 'd' && selectedId) {
        e.preventDefault();
        const el = design.elements.find((el) => el.id === selectedId);
        if (el) {
          addElement({ ...el, x: el.x + 20, y: el.y + 20 });
        }
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [
    selectedId,
    deleteElement,
    handleSave,
    handleUndo,
    handleRedo,
    design.elements,
    addElement,
  ]);

  /* ─── Sidebar Tabs ──────────────────────────────────────────── */

  const sidebarTabs: {
    key: SidebarTab;
    label: string;
    icon: React.ReactNode;
  }[] = [
    { key: 'uploads', label: 'My Uploads', icon: <IconUpload /> },
    { key: 'templates', label: 'Templates', icon: <IconTemplates /> },
    { key: 'media', label: 'Media', icon: <IconMedia /> },
    { key: 'text', label: 'Text', icon: <IconText /> },
    { key: 'shapes', label: 'Shapes', icon: <IconShapes /> },
    { key: 'background', label: 'Background', icon: <IconBackground /> },
    { key: 'layout', label: 'Layout', icon: <IconLayout /> },
    { key: 'layers', label: 'Layers', icon: <IconLayers /> },
    { key: 'audio', label: 'Audio', icon: <IconAudio /> },
    { key: 'draw', label: 'Draw', icon: <IconDraw /> },
  ];

  const renderSidebarContent = () => {
    switch (activeTab) {
      case 'uploads':
        return (
          <div style={panelStyles.container}>
            <h3 style={panelStyles.title}>My Uploads</h3>
            <div style={{ marginBottom: 16 }}>
              <label
                style={{
                  fontSize: 12,
                  color: '#666',
                  display: 'block',
                  marginBottom: 6,
                }}
              >
                Add Video by URL
              </label>
              <div style={{ display: 'flex', gap: 6 }}>
                <input
                  type="text"
                  placeholder="https://example.com/video.mp4"
                  style={{
                    flex: 1,
                    padding: '8px 12px',
                    border: '1px solid #e0e0e0',
                    borderRadius: 6,
                    fontSize: 13,
                  }}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      handleAddVideoByUrl((e.target as HTMLInputElement).value);
                      (e.target as HTMLInputElement).value = '';
                    }
                  }}
                />
                <button
                  style={{
                    padding: '8px 16px',
                    background: '#0078d4',
                    color: '#fff',
                    border: 'none',
                    borderRadius: 6,
                    fontSize: 13,
                    fontWeight: 600,
                    cursor: 'pointer',
                  }}
                  onClick={(e) => {
                    const input = e.currentTarget
                      .previousElementSibling as HTMLInputElement;
                    handleAddVideoByUrl(input.value);
                    input.value = '';
                  }}
                >
                  Add
                </button>
              </div>
            </div>
            <div
              style={{
                textAlign: 'center',
                padding: 32,
                color: '#999',
                fontSize: 13,
                borderTop: '1px solid #eee',
              }}
            >
              <div style={{ marginBottom: 12 }}>
                <IconUpload />
              </div>
              <p>Upload images and videos to use in your design.</p>
              <button
                style={{
                  ...panelStyles.addBtn,
                  justifyContent: 'center',
                  marginTop: 12,
                  width: '100%',
                  background: '#0078d4',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 6,
                  padding: '10px 16px',
                  fontWeight: 600,
                  cursor: 'pointer',
                }}
                onClick={handleUploadFiles}
              >
                Upload Files
              </button>
            </div>
          </div>
        );
      case 'templates':
        return (
          <TemplatesPanel templates={allTemplates} onSelect={applyTemplate} />
        );
      case 'media':
        return (
          <MediaPanel
            assets={hubAssets}
            onAddImage={handleAddImageFromHub}
            onAddVideo={handleAddVideoFromHub}
            onSearch={handleHubSearch}
            onFilterChange={handleHubFilterChange}
            onLoadMore={handleHubLoadMore}
            isLoading={hubLoading}
            hasMore={hubHasMore}
            activeFilter={hubFilter}
          />
        );
      case 'text':
        return <TextPanel onAddText={handleAddText} />;
      case 'shapes':
        return <ShapesPanel onAddShape={handleAddShape} />;
      case 'background':
        return (
          <BackgroundPanel
            bgColor={design.backgroundColor}
            onChange={(c) =>
              updateDesign((d) => ({ ...d, backgroundColor: c }))
            }
          />
        );
      case 'layout':
        return (
          <div style={panelStyles.container}>
            <h3 style={panelStyles.title}>Layout</h3>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                }}
              >
                <span style={{ fontSize: 13, color: '#333' }}>Grid</span>
                <div
                  style={{
                    width: 40,
                    height: 22,
                    borderRadius: 11,
                    background: showGrid ? '#0078d4' : '#ccc',
                    position: 'relative',
                    cursor: 'pointer',
                  }}
                  onClick={() => setShowGrid(!showGrid)}
                >
                  <div
                    style={{
                      width: 18,
                      height: 18,
                      borderRadius: 9,
                      background: '#fff',
                      position: 'absolute',
                      top: 2,
                      left: showGrid ? 20 : 2,
                      boxShadow: '0 1px 3px rgba(0,0,0,.2)',
                      transition: 'left 0.2s',
                    }}
                  />
                </div>
              </div>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                }}
              >
                <span style={{ fontSize: 13, color: '#333' }}>Folds</span>
                <span style={{ fontSize: 12, color: '#999' }}>None</span>
              </div>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                }}
              >
                <span style={{ fontSize: 13, color: '#333' }}>Bleed</span>
                <div
                  style={{
                    width: 40,
                    height: 22,
                    borderRadius: 11,
                    background: showBleed ? '#0078d4' : '#ccc',
                    position: 'relative',
                    cursor: 'pointer',
                  }}
                  onClick={() => setShowBleed(!showBleed)}
                >
                  <div
                    style={{
                      width: 18,
                      height: 18,
                      borderRadius: 9,
                      background: '#fff',
                      position: 'absolute',
                      top: 2,
                      left: showBleed ? 20 : 2,
                      boxShadow: '0 1px 3px rgba(0,0,0,.2)',
                      transition: 'left 0.2s',
                    }}
                  />
                </div>
              </div>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                }}
              >
                <span style={{ fontSize: 13, color: '#333' }}>
                  Alignment Guides
                </span>
                <div
                  style={{
                    width: 40,
                    height: 22,
                    borderRadius: 11,
                    background: showAlignmentGuides ? '#0078d4' : '#ccc',
                    position: 'relative',
                    cursor: 'pointer',
                  }}
                  onClick={() => setShowAlignmentGuides(!showAlignmentGuides)}
                >
                  <div
                    style={{
                      width: 18,
                      height: 18,
                      borderRadius: 9,
                      background: '#fff',
                      position: 'absolute',
                      top: 2,
                      left: showAlignmentGuides ? 20 : 2,
                      boxShadow: '0 1px 3px rgba(0,0,0,.2)',
                      transition: 'left 0.2s',
                    }}
                  />
                </div>
              </div>
            </div>
          </div>
        );
      case 'layers':
        return (
          <LayersPanel
            elements={design.elements}
            selectedId={selectedId}
            onSelect={setSelectedId}
            onMoveUp={moveLayerUp}
            onMoveDown={moveLayerDown}
            onDelete={deleteElement}
            onToggleLock={toggleLock}
          />
        );
      case 'draw':
        return (
          <div style={panelStyles.container}>
            <h3 style={panelStyles.title}>Draw</h3>
            <div
              style={{
                textAlign: 'center',
                padding: 32,
                color: '#999',
                fontSize: 13,
              }}
            >
              <div style={{ marginBottom: 12 }}>
                <IconDraw />
              </div>
              <p>Free-hand drawing tool coming soon.</p>
            </div>
          </div>
        );
      case 'audio':
        return (
          <div style={panelStyles.container}>
            <h3 style={panelStyles.title}>Audio</h3>
            <div style={{ marginBottom: 16 }}>
              <label
                style={{
                  fontSize: 12,
                  color: '#666',
                  display: 'block',
                  marginBottom: 6,
                }}
              >
                Add Audio by URL
              </label>
              <div style={{ display: 'flex', gap: 6 }}>
                <input
                  type="text"
                  placeholder="https://example.com/audio.mp3"
                  style={{
                    flex: 1,
                    padding: '6px 10px',
                    border: '1px solid #ddd',
                    borderRadius: 4,
                    fontSize: 13,
                  }}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      const input = e.currentTarget;
                      if (input.value.trim()) {
                        const audioEl: DesignElement = {
                          id: uid(),
                          type: 'audio',
                          x: 50,
                          y: 50,
                          width: 300,
                          height: 40,
                          fill: '#0078d4',
                          audioUrl: input.value.trim(),
                          volume: 1,
                          fadeIn: 0,
                          fadeOut: 0,
                          startTime: 0,
                          duration: 0,
                        };
                        setDesign({
                          ...design,
                          elements: [...design.elements, audioEl],
                        });
                        setSelectedElementId(audioEl.id);
                        input.value = '';
                      }
                    }
                  }}
                />
              </div>
            </div>
            <div
              style={{
                padding: '24px 16px',
                textAlign: 'center',
                color: '#999',
                fontSize: 13,
                borderTop: '1px solid #eee',
              }}
            >
              <IconAudio />
              <p style={{ marginTop: 12 }}>Upload audio files or browse library</p>
              <button
                style={{
                  ...panelStyles.button,
                  marginTop: 12,
                }}
                onClick={() => {
                  // TODO: Implement audio upload
                  console.log('Audio upload coming soon');
                }}
              >
                Upload Audio
              </button>
            </div>
          </div>
        );
      default:
        return null;
    }
  };

  /* ─── Render ────────────────────────────────────────────────── */

  return (
    <div style={styles.root}>
      {/* ── Top toolbar (PMW blue gradient) ──────────── */}
      <div style={styles.toolbar}>
        <div style={styles.toolbarLeft}>
          <button
            style={styles.toolbarIconBtn}
            title="Back"
            onClick={handleBack}
          >
            <IconBack />
          </button>
          <span style={styles.logo}>Design Builder</span>
          <button
            style={styles.toolbarTextBtn}
            onClick={() => setShowFileMenu(!showFileMenu)}
          >
            File
          </button>
          {showFileMenu && (
            <div
              style={{
                position: 'absolute',
                top: 50,
                left: 200,
                background: '#fff',
                border: '1px solid #e0e0e0',
                borderRadius: 8,
                boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
                zIndex: 1000,
                minWidth: 180,
              }}
            >
              <button
                onClick={handleNewDesign}
                style={{
                  display: 'block',
                  width: '100%',
                  padding: '10px 16px',
                  border: 'none',
                  background: 'transparent',
                  textAlign: 'left',
                  cursor: 'pointer',
                  fontSize: 13,
                }}
                onMouseEnter={(e) =>
                  (e.currentTarget.style.background = '#f5f5f5')
                }
                onMouseLeave={(e) =>
                  (e.currentTarget.style.background = 'transparent')
                }
              >
                New Design
              </button>
              <button
                onClick={() => {
                  void handleSave();
                  setShowFileMenu(false);
                }}
                style={{
                  display: 'block',
                  width: '100%',
                  padding: '10px 16px',
                  border: 'none',
                  background: 'transparent',
                  textAlign: 'left',
                  cursor: 'pointer',
                  fontSize: 13,
                }}
                onMouseEnter={(e) =>
                  (e.currentTarget.style.background = '#f5f5f5')
                }
                onMouseLeave={(e) =>
                  (e.currentTarget.style.background = 'transparent')
                }
              >
                Save
              </button>
              <button
                onClick={() => {
                  void handleExport('png');
                  setShowFileMenu(false);
                }}
                style={{
                  display: 'block',
                  width: '100%',
                  padding: '10px 16px',
                  border: 'none',
                  background: 'transparent',
                  textAlign: 'left',
                  cursor: 'pointer',
                  fontSize: 13,
                }}
                onMouseEnter={(e) =>
                  (e.currentTarget.style.background = '#f5f5f5')
                }
                onMouseLeave={(e) =>
                  (e.currentTarget.style.background = 'transparent')
                }
              >
                Export as PNG
              </button>
              <button
                onClick={() => {
                  void handleExport('svg');
                  setShowFileMenu(false);
                }}
                style={{
                  display: 'block',
                  width: '100%',
                  padding: '10px 16px',
                  border: 'none',
                  background: 'transparent',
                  textAlign: 'left',
                  cursor: 'pointer',
                  fontSize: 13,
                }}
                onMouseEnter={(e) =>
                  (e.currentTarget.style.background = '#f5f5f5')
                }
                onMouseLeave={(e) =>
                  (e.currentTarget.style.background = 'transparent')
                }
              >
                Export as SVG
              </button>
            </div>
          )}
          <button
            style={styles.toolbarTextBtn}
            onClick={() => setShowSizeModal(true)}
          >
            Resize
          </button>
          <div style={{ display: 'flex', gap: 4, marginLeft: 8 }}>
            <button
              style={{
                ...styles.toolbarIconBtn,
                opacity: historyIndex > 0 ? 1 : 0.4,
              }}
              title="Undo (Ctrl+Z)"
              onClick={handleUndo}
              disabled={historyIndex <= 0}
            >
              <IconUndo />
            </button>
            <button
              style={{
                ...styles.toolbarIconBtn,
                opacity: historyIndex < history.length - 1 ? 1 : 0.4,
              }}
              title="Redo (Ctrl+Y)"
              onClick={handleRedo}
              disabled={historyIndex >= history.length - 1}
            >
              <IconRedo />
            </button>
          </div>
        </div>
        <div style={styles.toolbarRight}>
          <button
            style={styles.toolbarIconBtn}
            title="Help"
            onClick={() => setShowHelpModal(true)}
          >
            <IconHelp />
          </button>
          <button style={styles.saveBtn} onClick={handleSave}>
            <IconSave /> Save{hasUnsavedChanges ? ' •' : ''}
          </button>
          <button
            style={styles.shareBtn}
            onClick={() => setShowShareModal(true)}
          >
            <IconShare /> Share
          </button>
          <button
            style={styles.downloadBtn}
            onClick={() => handleExport('png')}
          >
            <IconDownload /> Download
          </button>
          <button
            style={styles.publishBtn}
            onClick={() => setShowPublishModal(true)}
          >
            <IconPublish /> Publish
          </button>
        </div>
      </div>

      {/* ── Main area ──────────────────────────────────── */}
      <div style={styles.mainArea}>
        {/* Left sidebar icon rail */}
        <div style={styles.sidebarTabs}>
          {sidebarTabs.map((t) => (
            <button
              key={t.key}
              onClick={() => setActiveTab(t.key)}
              style={{
                ...styles.sidebarTabBtn,
                backgroundColor:
                  activeTab === t.key ? '#e8f4fd' : 'transparent',
                color: activeTab === t.key ? '#1976d2' : '#555',
                borderLeft:
                  activeTab === t.key
                    ? '3px solid #1976d2'
                    : '3px solid transparent',
              }}
              title={t.label}
            >
              {t.icon}
              <span
                style={{
                  fontSize: 9,
                  marginTop: 2,
                  fontWeight: activeTab === t.key ? 600 : 400,
                }}
              >
                {t.label}
              </span>
            </button>
          ))}
        </div>

        {/* Left sidebar panel */}
        <div style={styles.sidebarPanel}>{renderSidebarContent()}</div>

        {/* Canvas area */}
        <div style={styles.canvasContainer}>
          <div style={styles.canvasScroll}>
            <CanvasRenderer
              design={design}
              selectedId={selectedId}
              onSelectElement={setSelectedId}
              onMoveElement={handleMoveElement}
              canvasScale={canvasScale}
            />
          </div>
          {/* Floating zoom controls (bottom-right like PMW) */}
          <div style={styles.zoomFloat}>
            <button
              style={styles.zoomBtn}
              onClick={() => setCanvasScale((s) => Math.min(2, s + 0.1))}
              title="Zoom in"
            >
              <IconZoomIn />
            </button>
            <button
              style={styles.zoomBtn}
              onClick={() => setCanvasScale((s) => Math.max(0.1, s - 0.1))}
              title="Zoom out"
            >
              <IconZoomOut />
            </button>
          </div>
        </div>

        {/* Right properties panel (PMW "Design" panel) */}
        <div style={styles.rightPanel}>
          <div style={styles.rightPanelHeader}>Design</div>

          {/* Size section */}
          <div style={styles.rightSection}>
            <div style={styles.rightSectionRow}>
              <span style={styles.rightLabel}>Size</span>
              <button
                style={styles.rightValueBtn}
                onClick={() => setShowSizeModal(true)}
              >
                <span style={{ fontWeight: 600, fontSize: 13 }}>
                  Custom Size
                </span>
                <span style={{ fontSize: 11, color: '#888' }}>
                  {design.width}px × {design.height}px
                </span>
              </button>
            </div>
          </div>

          {/* Styles section */}
          <div style={styles.rightSection}>
            <div style={styles.rightSectionRow}>
              <span style={styles.rightLabel}>Styles</span>
              <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                <div
                  style={{
                    width: 32,
                    height: 32,
                    borderRadius: 6,
                    border: '1px solid #ddd',
                    background: design.backgroundColor,
                  }}
                />
                <button
                  style={{
                    width: 32,
                    height: 32,
                    borderRadius: 6,
                    border: '1px solid #ddd',
                    background: '#fff',
                    fontSize: 18,
                    cursor: 'pointer',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                  }}
                  onClick={() =>
                    alert(
                      'Add custom style preset (save current colors, fonts, etc.)',
                    )
                  }
                  title="Add style preset"
                >
                  +
                </button>
              </div>
            </div>
          </div>

          {/* Background section */}
          <div style={styles.rightSection}>
            <div style={styles.rightSectionRow}>
              <span style={styles.rightLabel}>Background</span>
              <span style={{ fontSize: 12, color: '#555' }}>Solid Color</span>
            </div>
            <div style={{ padding: '8px 0' }}>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                }}
              >
                <span style={{ fontSize: 12, color: '#888' }}>Color</span>
                <input
                  type="color"
                  value={design.backgroundColor}
                  onChange={(e) =>
                    updateDesign((d) => ({
                      ...d,
                      backgroundColor: e.target.value,
                    }))
                  }
                  title="Canvas background color"
                  style={{
                    width: 32,
                    height: 24,
                    border: '1px solid #ddd',
                    borderRadius: 4,
                    cursor: 'pointer',
                    padding: 0,
                  }}
                />
              </div>
            </div>
          </div>

          {/* Animation section */}
          <div style={styles.rightSection}>
            <span style={styles.rightLabel}>Animation</span>
            <div style={{ display: 'flex', gap: 16, marginTop: 8 }}>
              <div style={{ textAlign: 'center' }}>
                <div
                  style={{
                    width: 48,
                    height: 48,
                    borderRadius: 8,
                    border: '2px dashed #ccc',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    cursor: 'pointer',
                  }}
                  onClick={() => setShowAnimationModal(true)}
                >
                  <svg
                    width="20"
                    height="20"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="#999"
                    strokeWidth="1.5"
                  >
                    <circle cx="12" cy="12" r="10" />
                    <path d="M12 8v4l3 3" />
                  </svg>
                </div>
                <span
                  style={{
                    fontSize: 11,
                    color: '#888',
                    marginTop: 4,
                    display: 'block',
                  }}
                >
                  Start
                </span>
              </div>
              <div style={{ textAlign: 'center' }}>
                <div
                  style={{
                    width: 48,
                    height: 48,
                    borderRadius: 8,
                    border: '2px dashed #ccc',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    cursor: 'pointer',
                  }}
                  onClick={() => setShowAnimationModal(true)}
                >
                  <svg
                    width="20"
                    height="20"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="#999"
                    strokeWidth="1.5"
                  >
                    <circle cx="12" cy="12" r="10" />
                    <path d="M12 8v4l3 3" />
                  </svg>
                </div>
                <span
                  style={{
                    fontSize: 11,
                    color: '#888',
                    marginTop: 4,
                    display: 'block',
                  }}
                >
                  End
                </span>
              </div>
            </div>
          </div>

          {/* Title section */}
          <div style={styles.rightSection}>
            <span style={styles.rightLabel}>Title</span>
            <input
              type="text"
              value={designName}
              onChange={(e) => setDesignName(e.target.value)}
              style={{
                width: '100%',
                padding: '8px 10px',
                border: '1px solid #e0e0e0',
                borderRadius: 6,
                fontSize: 13,
                marginTop: 6,
                boxSizing: 'border-box',
              }}
              placeholder="add text"
            />
          </div>

          {/* Selected element properties */}
          {selectedElement && (
            <div style={{ borderTop: '1px solid #eee' }}>
              <PropertiesPanel
                element={selectedElement}
                onChange={(updates) => {
                  if (selectedId) updateElement(selectedId, updates);
                }}
              />
            </div>
          )}

          {/* Shortcuts */}
          <div
            style={{
              ...panelStyles.container,
              marginTop: 8,
              borderTop: '1px solid #eee',
            }}
          >
            <h3 style={panelStyles.title}>Shortcuts</h3>
            <div style={{ fontSize: 11, color: '#888', lineHeight: 1.8 }}>
              <div>
                <kbd style={kbdStyle}>Ctrl+S</kbd> Save
              </div>
              <div>
                <kbd style={kbdStyle}>Ctrl+D</kbd> Duplicate
              </div>
              <div>
                <kbd style={kbdStyle}>Delete</kbd> Remove
              </div>
              <div>
                <kbd style={kbdStyle}>Esc</kbd> Deselect
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* ── Bottom timeline bar (PMW-style) ────────── */}
      <div style={styles.bottomBar}>
        <div style={styles.bottomLeft}>
          <span style={{ fontSize: 12, color: '#555', fontWeight: 500 }}>
            00:00 / 00:00
          </span>
          <button
            style={styles.bottomBtn}
            onClick={() => setShowTimelineModal(true)}
          >
            <IconTimeline /> Edit timeline
          </button>
        </div>
        <div style={styles.bottomRight}>
          <button
            style={styles.bottomIconBtn}
            title="Music"
            onClick={() => setShowMusicModal(true)}
          >
            <IconMusic />
          </button>
        </div>
      </div>

      {/* ── Share Modal ──────────────────────────── */}
      {showShareModal && (
        <div
          style={styles.modalOverlay}
          onClick={() => setShowShareModal(false)}
        >
          <div style={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h2 style={{ margin: '0 0 16px', fontSize: 20 }}>Share Design</h2>
            <p style={{ color: '#666', fontSize: 13, marginBottom: 16 }}>
              Share your design with others
            </p>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <button
                style={{
                  padding: '12px',
                  border: '1px solid #e0e0e0',
                  borderRadius: 6,
                  background: '#fff',
                  cursor: 'pointer',
                  fontSize: 14,
                  textAlign: 'left',
                }}
                onClick={() => alert('Copy link functionality')}
              >
                📋 Copy link
              </button>
              <button
                style={{
                  padding: '12px',
                  border: '1px solid #e0e0e0',
                  borderRadius: 6,
                  background: '#fff',
                  cursor: 'pointer',
                  fontSize: 14,
                  textAlign: 'left',
                }}
                onClick={() => alert('Email share functionality')}
              >
                ✉️ Share via email
              </button>
              <button
                style={{
                  padding: '12px',
                  border: '1px solid #e0e0e0',
                  borderRadius: 6,
                  background: '#fff',
                  cursor: 'pointer',
                  fontSize: 14,
                  textAlign: 'left',
                }}
                onClick={() => alert('Social share functionality')}
              >
                🌐 Share on social media
              </button>
            </div>
            <div
              style={{
                display: 'flex',
                justifyContent: 'flex-end',
                marginTop: 16,
              }}
            >
              <button
                style={styles.secondaryBtn}
                onClick={() => setShowShareModal(false)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Help Modal ──────────────────────────── */}
      {showHelpModal && (
        <div
          style={styles.modalOverlay}
          onClick={() => setShowHelpModal(false)}
        >
          <div style={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h2 style={{ margin: '0 0 16px', fontSize: 20 }}>
              Help & Tutorials
            </h2>
            <div style={{ maxHeight: 400, overflowY: 'auto' }}>
              <h3 style={{ fontSize: 15, marginTop: 16 }}>Getting Started</h3>
              <ul style={{ fontSize: 13, color: '#666', lineHeight: 1.8 }}>
                <li>Use the left sidebar to add elements to your design</li>
                <li>Click on any element to select and edit it</li>
                <li>Drag elements to move them around the canvas</li>
                <li>
                  Use the right panel to customize colors, sizes, and properties
                </li>
              </ul>
              <h3 style={{ fontSize: 15, marginTop: 16 }}>
                Keyboard Shortcuts
              </h3>
              <ul style={{ fontSize: 13, color: '#666', lineHeight: 1.8 }}>
                <li>
                  <strong>Ctrl+S</strong> - Save design
                </li>
                <li>
                  <strong>Ctrl+Z</strong> - Undo
                </li>
                <li>
                  <strong>Ctrl+Y</strong> - Redo
                </li>
                <li>
                  <strong>Ctrl+D</strong> - Duplicate selected element
                </li>
                <li>
                  <strong>Delete</strong> - Remove selected element
                </li>
                <li>
                  <strong>Esc</strong> - Deselect
                </li>
              </ul>
              <h3 style={{ fontSize: 15, marginTop: 16 }}>Need More Help?</h3>
              <p style={{ fontSize: 13, color: '#666' }}>
                Visit our documentation or contact support for assistance.
              </p>
            </div>
            <div
              style={{
                display: 'flex',
                justifyContent: 'flex-end',
                marginTop: 16,
              }}
            >
              <button
                style={styles.secondaryBtn}
                onClick={() => setShowHelpModal(false)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Publish Modal ──────────────────────────── */}
      {showPublishModal && (
        <div
          style={styles.modalOverlay}
          onClick={() => setShowPublishModal(false)}
        >
          <div style={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h2 style={{ margin: '0 0 16px', fontSize: 20 }}>Publish Design</h2>
            <p style={{ color: '#666', fontSize: 13, marginBottom: 16 }}>
              Choose how you want to publish your design
            </p>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <button
                style={{
                  padding: '12px',
                  border: '1px solid #e0e0e0',
                  borderRadius: 6,
                  background: '#fff',
                  cursor: 'pointer',
                  fontSize: 14,
                  textAlign: 'left',
                }}
                onClick={() => alert('Gallery publish functionality')}
              >
                🖼️ Publish to gallery
              </button>
              <button
                style={{
                  padding: '12px',
                  border: '1px solid #e0e0e0',
                  borderRadius: 6,
                  background: '#fff',
                  cursor: 'pointer',
                  fontSize: 14,
                  textAlign: 'left',
                }}
                onClick={() => alert('Print order functionality')}
              >
                🖨️ Order prints
              </button>
              <button
                style={{
                  padding: '12px',
                  border: '1px solid #e0e0e0',
                  borderRadius: 6,
                  background: '#fff',
                  cursor: 'pointer',
                  fontSize: 14,
                  textAlign: 'left',
                }}
                onClick={() => alert('Schedule post functionality')}
              >
                📅 Schedule social post
              </button>
            </div>
            <div
              style={{
                display: 'flex',
                justifyContent: 'flex-end',
                marginTop: 16,
              }}
            >
              <button
                style={styles.secondaryBtn}
                onClick={() => setShowPublishModal(false)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Timeline Modal ──────────────────────────── */}
      {showTimelineModal && (
        <div
          style={styles.modalOverlay}
          onClick={() => setShowTimelineModal(false)}
        >
          <div
            style={{ ...styles.modal, maxWidth: 800 }}
            onClick={(e) => e.stopPropagation()}
          >
            <h2 style={{ margin: '0 0 16px', fontSize: 20 }}>
              Timeline Editor
            </h2>
            <p style={{ color: '#666', fontSize: 13, marginBottom: 16 }}>
              Arrange elements and set animation timing
            </p>
            <div
              style={{
                background: '#f9f9f9',
                padding: 24,
                borderRadius: 8,
                minHeight: 200,
              }}
            >
              <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
                {design.elements.map((el, i) => (
                  <div
                    key={el.id}
                    style={{
                      padding: '8px 12px',
                      background: '#fff',
                      border: '1px solid #ddd',
                      borderRadius: 4,
                      fontSize: 12,
                    }}
                  >
                    {el.type} {i + 1}
                  </div>
                ))}
              </div>
              <div
                style={{
                  color: '#888',
                  fontSize: 13,
                  textAlign: 'center',
                  marginTop: 32,
                }}
              >
                Drag elements to adjust timing and order
              </div>
            </div>
            <div
              style={{
                display: 'flex',
                justifyContent: 'flex-end',
                marginTop: 16,
              }}
            >
              <button
                style={styles.secondaryBtn}
                onClick={() => setShowTimelineModal(false)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Music Modal ──────────────────────────── */}
      {showMusicModal && (
        <div
          style={styles.modalOverlay}
          onClick={() => setShowMusicModal(false)}
        >
          <div style={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h2 style={{ margin: '0 0 16px', fontSize: 20 }}>Add Music</h2>
            <p style={{ color: '#666', fontSize: 13, marginBottom: 16 }}>
              Add background music to your design
            </p>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <button
                style={{
                  padding: '12px',
                  border: '1px solid #e0e0e0',
                  borderRadius: 6,
                  background: '#fff',
                  cursor: 'pointer',
                  fontSize: 14,
                  textAlign: 'left',
                }}
                onClick={handleUploadFiles}
              >
                📁 Upload audio file
              </button>
              <button
                style={{
                  padding: '12px',
                  border: '1px solid #e0e0e0',
                  borderRadius: 6,
                  background: '#fff',
                  cursor: 'pointer',
                  fontSize: 14,
                  textAlign: 'left',
                }}
                onClick={() => alert('Browse audio library')}
              >
                🎵 Browse audio library
              </button>
              <button
                style={{
                  padding: '12px',
                  border: '1px solid #e0e0e0',
                  borderRadius: 6,
                  background: '#fff',
                  cursor: 'pointer',
                  fontSize: 14,
                  textAlign: 'left',
                }}
                onClick={() => alert('Record audio')}
              >
                🎤 Record audio
              </button>
            </div>
            <div
              style={{
                display: 'flex',
                justifyContent: 'flex-end',
                marginTop: 16,
              }}
            >
              <button
                style={styles.secondaryBtn}
                onClick={() => setShowMusicModal(false)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Animation Modal ──────────────────────────── */}
      {showAnimationModal && (
        <div
          style={styles.modalOverlay}
          onClick={() => setShowAnimationModal(false)}
        >
          <div style={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h2 style={{ margin: '0 0 16px', fontSize: 20 }}>Animations</h2>
            <p style={{ color: '#666', fontSize: 13, marginBottom: 16 }}>
              Add entrance and exit animations
            </p>
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(3, 1fr)',
                gap: 12,
              }}
            >
              {[
                'Fade In',
                'Slide In',
                'Bounce',
                'Zoom In',
                'Rotate',
                'Flip',
              ].map((anim) => (
                <button
                  key={anim}
                  style={{
                    padding: '16px',
                    border: '1px solid #e0e0e0',
                    borderRadius: 6,
                    background: '#fff',
                    cursor: 'pointer',
                    fontSize: 13,
                    textAlign: 'center',
                  }}
                  onClick={() => {
                    alert(`Apply ${anim} animation`);
                    setShowAnimationModal(false);
                  }}
                >
                  {anim}
                </button>
              ))}
            </div>
            <div
              style={{
                display: 'flex',
                justifyContent: 'flex-end',
                marginTop: 16,
              }}
            >
              <button
                style={styles.secondaryBtn}
                onClick={() => setShowAnimationModal(false)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Size Preset Modal ──────────────────────────── */}
      {showSizeModal && (
        <div
          style={styles.modalOverlay}
          onClick={() => setShowSizeModal(false)}
        >
          <div style={styles.modal} onClick={(e) => e.stopPropagation()}>
            <h2 style={{ margin: '0 0 16px', fontSize: 20 }}>
              Choose Canvas Size
            </h2>
            {['Social Media', 'Print', 'Web', 'Presentation', 'Custom'].map(
              (cat) => (
                <div key={cat}>
                  <h4
                    style={{
                      margin: '12px 0 6px',
                      color: '#555',
                      fontSize: 13,
                      textTransform: 'uppercase',
                      letterSpacing: 1,
                    }}
                  >
                    {cat}
                  </h4>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                    {props.sizePresets
                      .filter((p) => p.category === cat)
                      .map((p) => (
                        <button
                          key={p.name}
                          onClick={() => handleResize(p)}
                          style={{
                            ...styles.sizePresetBtn,
                            borderColor:
                              design.width === p.width &&
                              design.height === p.height
                                ? '#0078d4'
                                : '#e0e0e0',
                          }}
                        >
                          <span style={{ fontWeight: 600, fontSize: 13 }}>
                            {p.name}
                          </span>
                          <span style={{ fontSize: 11, color: '#888' }}>
                            {p.width}×{p.height}
                          </span>
                        </button>
                      ))}
                  </div>
                </div>
              ),
            )}
            <div
              style={{
                display: 'flex',
                justifyContent: 'flex-end',
                marginTop: 16,
              }}
            >
              <button
                style={styles.secondaryBtn}
                onClick={() => setShowSizeModal(false)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

/* Alias for Kentico template resolution */
export const DesignBuilderLayout = DesignBuilderLayoutTemplate;

/* ─── Styles ──────────────────────────────────────────────────── */

const kbdStyle: React.CSSProperties = {
  display: 'inline-block',
  padding: '1px 5px',
  fontSize: 10,
  backgroundColor: '#f0f0f0',
  borderRadius: 3,
  border: '1px solid #ccc',
  marginRight: 4,
  fontFamily: 'monospace',
};

const styles: Record<string, React.CSSProperties> = {
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    fontFamily: "'Inter', 'Segoe UI', -apple-system, sans-serif",
    overflow: 'hidden',
    backgroundColor: '#f5f0eb',
  },
  /* ── Toolbar (PMW blue gradient) ─── */
  toolbar: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '0 12px',
    background: 'linear-gradient(135deg, #42a5f5 0%, #1976d2 100%)',
    color: '#fff',
    minHeight: 48,
    zIndex: 100,
  },
  toolbarLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: 4,
  },
  toolbarRight: {
    display: 'flex',
    alignItems: 'center',
    gap: 6,
  },
  logo: {
    fontSize: 15,
    fontWeight: 700,
    letterSpacing: -0.3,
    marginRight: 8,
  },
  toolbarIconBtn: {
    background: 'none',
    border: 'none',
    color: '#fff',
    padding: '6px 8px',
    cursor: 'pointer',
    borderRadius: 4,
    display: 'flex',
    alignItems: 'center',
  },
  toolbarTextBtn: {
    background: 'none',
    border: 'none',
    color: '#fff',
    padding: '6px 12px',
    cursor: 'pointer',
    fontSize: 13,
    fontWeight: 500,
    borderRadius: 4,
  },
  saveBtn: {
    display: 'flex',
    alignItems: 'center',
    gap: 5,
    background: 'rgba(255,255,255,0.15)',
    border: '1px solid rgba(255,255,255,0.3)',
    color: '#fff',
    padding: '6px 14px',
    borderRadius: 4,
    cursor: 'pointer',
    fontSize: 13,
    fontWeight: 500,
  },
  shareBtn: {
    display: 'flex',
    alignItems: 'center',
    gap: 5,
    background: 'rgba(255,255,255,0.15)',
    border: '1px solid rgba(255,255,255,0.3)',
    color: '#fff',
    padding: '6px 14px',
    borderRadius: 4,
    cursor: 'pointer',
    fontSize: 13,
    fontWeight: 500,
  },
  downloadBtn: {
    display: 'flex',
    alignItems: 'center',
    gap: 5,
    background: 'rgba(255,255,255,0.2)',
    border: '1px solid rgba(255,255,255,0.35)',
    color: '#fff',
    padding: '6px 14px',
    borderRadius: 4,
    cursor: 'pointer',
    fontSize: 13,
    fontWeight: 500,
  },
  publishBtn: {
    display: 'flex',
    alignItems: 'center',
    gap: 5,
    background: '#fff',
    border: 'none',
    color: '#1976d2',
    padding: '7px 18px',
    borderRadius: 20,
    cursor: 'pointer',
    fontSize: 13,
    fontWeight: 700,
  },
  /* ── Main area ─── */
  mainArea: {
    display: 'flex',
    flex: 1,
    overflow: 'hidden',
  },
  /* ── Left sidebar (PMW white rail) ─── */
  sidebarTabs: {
    display: 'flex',
    flexDirection: 'column',
    width: 76,
    backgroundColor: '#fff',
    borderRight: '1px solid #e8e8e8',
    paddingTop: 4,
    overflowY: 'auto',
  },
  sidebarTabBtn: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: 1,
    padding: '10px 4px',
    border: 'none',
    cursor: 'pointer',
    transition: 'all 0.15s',
    borderRadius: 0,
  },
  sidebarPanel: {
    width: 280,
    backgroundColor: '#fff',
    borderRight: '1px solid #e8e8e8',
    overflowY: 'auto',
  },
  /* ── Canvas ─── */
  canvasContainer: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
    backgroundColor: '#f5f0eb',
    position: 'relative',
  },
  canvasScroll: {
    flex: 1,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'auto',
    padding: 40,
  },
  /* ── Zoom (floating bottom-right) ─── */
  zoomFloat: {
    position: 'absolute',
    bottom: 16,
    right: 16,
    display: 'flex',
    flexDirection: 'column',
    gap: 4,
    zIndex: 20,
  },
  zoomBtn: {
    width: 36,
    height: 36,
    borderRadius: 8,
    background: '#fff',
    border: '1px solid #ddd',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 0,
  },
  /* ── Right panel ─── */
  rightPanel: {
    width: 280,
    backgroundColor: '#fff',
    borderLeft: '1px solid #e8e8e8',
    overflowY: 'auto',
    padding: 0,
  },
  rightPanelHeader: {
    fontSize: 15,
    fontWeight: 600,
    textAlign: 'center',
    padding: '14px 0',
    borderBottom: '1px solid #eee',
    color: '#333',
  },
  rightSection: {
    padding: '12px 16px',
    borderBottom: '1px solid #f0f0f0',
  },
  rightSectionRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  rightLabel: {
    fontSize: 13,
    color: '#555',
    fontWeight: 500,
  },
  rightValueBtn: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'flex-end',
    background: 'none',
    border: '1px solid #e0e0e0',
    borderRadius: 6,
    padding: '6px 10px',
    cursor: 'pointer',
  },
  /* ── Bottom bar ─── */
  bottomBar: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '6px 16px',
    backgroundColor: '#fff',
    borderTop: '1px solid #e8e8e8',
    minHeight: 36,
    zIndex: 100,
  },
  bottomLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: 12,
  },
  bottomRight: {
    display: 'flex',
    alignItems: 'center',
    gap: 8,
  },
  bottomBtn: {
    display: 'flex',
    alignItems: 'center',
    gap: 6,
    background: 'none',
    border: 'none',
    color: '#555',
    cursor: 'pointer',
    fontSize: 12,
    padding: '4px 8px',
    borderRadius: 4,
  },
  bottomIconBtn: {
    background: '#f5f5f5',
    border: '1px solid #e0e0e0',
    borderRadius: 6,
    width: 32,
    height: 32,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    cursor: 'pointer',
    padding: 0,
  },
  /* ── Modal ─── */
  modalOverlay: {
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0,0,0,0.5)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 1000,
  },
  modal: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 24,
    maxWidth: 680,
    maxHeight: '80vh',
    overflowY: 'auto',
    boxShadow: '0 12px 48px rgba(0,0,0,0.2)',
  },
  sizePresetBtn: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    padding: '10px 16px',
    border: '2px solid #e0e0e0',
    borderRadius: 8,
    background: '#fff',
    cursor: 'pointer',
    minWidth: 120,
    transition: 'border-color 0.15s',
  },
  secondaryBtn: {
    backgroundColor: '#f5f5f5',
    color: '#333',
    border: '1px solid #ddd',
    padding: '8px 18px',
    borderRadius: 6,
    cursor: 'pointer',
    fontSize: 13,
    fontWeight: 500,
  },
};

const panelStyles: Record<string, React.CSSProperties> = {
  container: {
    padding: 14,
  },
  title: {
    fontSize: 14,
    fontWeight: 700,
    marginBottom: 12,
    color: '#333',
    margin: '0 0 12px',
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: 8,
  },
  templateCard: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: 6,
    padding: 10,
    border: '1px solid #e0e0e0',
    borderRadius: 6,
    background: '#fff',
    cursor: 'pointer',
    transition: 'border-color 0.15s',
  },
  templatePreview: {
    width: '100%',
    height: 60,
    backgroundColor: '#f5f5f5',
    borderRadius: 4,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  templateLabel: {
    fontSize: 12,
    fontWeight: 600,
    textAlign: 'center',
  },
  templateCategory: {
    fontSize: 10,
    color: '#999',
  },
  addBtn: {
    display: 'flex',
    alignItems: 'center',
    padding: '12px 14px',
    border: '1px solid #e0e0e0',
    borderRadius: 6,
    background: '#fafafa',
    cursor: 'pointer',
    transition: 'background 0.15s',
  },
  shapeBtn: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: 4,
    padding: 12,
    border: '1px solid #e0e0e0',
    borderRadius: 6,
    background: '#fff',
    cursor: 'pointer',
  },
  label: {
    fontSize: 12,
    fontWeight: 600,
    display: 'block',
    marginBottom: 6,
    color: '#555',
  },
  colorInput: {
    padding: '4px 8px',
    border: '1px solid #ddd',
    borderRadius: 4,
    fontSize: 12,
    fontFamily: 'monospace',
    width: 80,
  },
  swatchGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(5, 1fr)',
    gap: 6,
  },
  swatch: {
    width: 32,
    height: 32,
    borderRadius: 4,
    cursor: 'pointer',
    transition: 'transform 0.1s',
  },
  layerItem: {
    display: 'flex',
    alignItems: 'center',
    gap: 6,
    padding: '6px 8px',
    border: '1px solid #e0e0e0',
    borderRadius: 4,
    cursor: 'pointer',
    fontSize: 12,
  },
  iconBtn: {
    background: 'none',
    border: 'none',
    padding: '2px 4px',
    cursor: 'pointer',
    fontSize: 11,
    lineHeight: 1,
  },
  propGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: 4,
  },
  propLabel: {
    fontSize: 11,
    fontWeight: 600,
    color: '#555',
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  microLabel: {
    fontSize: 10,
    color: '#999',
    alignSelf: 'center',
  },
  numInput: {
    width: 64,
    padding: '4px 6px',
    border: '1px solid #ddd',
    borderRadius: 4,
    fontSize: 12,
  },
  textArea: {
    width: '100%',
    padding: '6px 8px',
    border: '1px solid #ddd',
    borderRadius: 4,
    fontSize: 13,
    fontFamily: 'inherit',
    resize: 'vertical',
  },
  selectInput: {
    width: '100%',
    padding: '6px 8px',
    border: '1px solid #ddd',
    borderRadius: 4,
    fontSize: 12,
    background: '#fff',
  },
};
