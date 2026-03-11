/* DesignListLayout – My Designs gallery page for Kentico admin */
import React, { useState } from 'react';
import { usePageCommand } from '@kentico/xperience-admin-base';
import {
  Button,
  ButtonColor,
  Headline,
  HeadlineSize,
  Spacing,
  Stack,
} from '@kentico/xperience-admin-components';

type DesignSummary = {
  id: number;
  name: string;
  previewUrl: string;
  lastModified: string;
  width: number;
  height: number;
};

interface DesignListClientProperties {
  designs: DesignSummary[];
}

export const DesignListLayoutTemplate = (props: DesignListClientProperties) => {
  const [designs, setDesigns] = useState(props.designs);
  const [searchTerm, setSearchTerm] = useState('');

  const { execute: deleteDesign } = usePageCommand<void, { designId: number }>(
    'DELETE_DESIGN',
    {
      after() {
        // Reload would happen here
      },
    },
  );

  const { execute: duplicateDesign } = usePageCommand<void, { designId: number }>(
    'DUPLICATE_DESIGN',
  );

  const filteredDesigns = designs.filter((d) =>
    d.name.toLowerCase().includes(searchTerm.toLowerCase()),
  );

  return (
    <div style={styles.root}>
      {/* Header */}
      <div style={styles.header}>
        <div style={styles.headerLeft}>
          <Headline size={HeadlineSize.L}>🎨 My Designs</Headline>
          <span style={styles.count}>{designs.length} designs</span>
        </div>
        <div style={styles.headerRight}>
          <input
            type="text"
            placeholder="Search designs..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            style={styles.searchInput}
          />
          <a
            href="/admin/design-builder/editor"
            style={styles.createBtn}
          >
            + New Design
          </a>
        </div>
      </div>

      {/* Grid */}
      {filteredDesigns.length > 0 ? (
        <div style={styles.grid}>
          {filteredDesigns.map((d) => (
            <div key={d.id} style={styles.card}>
              <div style={styles.cardPreview}>
                {d.previewUrl ? (
                  <img src={d.previewUrl} alt={d.name} style={styles.previewImg} />
                ) : (
                  <div style={styles.placeholder}>
                    <span style={{ fontSize: 40 }}>📄</span>
                    <span style={{ fontSize: 12, color: '#999' }}>
                      {d.width} × {d.height}
                    </span>
                  </div>
                )}
              </div>
              <div style={styles.cardBody}>
                <h3 style={styles.cardTitle}>{d.name}</h3>
                <span style={styles.cardMeta}>
                  {d.width}×{d.height} · {d.lastModified}
                </span>
                <div style={styles.cardActions}>
                  <a
                    href={`/admin/design-builder/editor?id=${d.id}`}
                    style={styles.editBtn}
                  >
                    Edit
                  </a>
                  <button
                    style={styles.actionBtn}
                    onClick={() => duplicateDesign({ designId: d.id })}
                  >
                    Duplicate
                  </button>
                  <button
                    style={{
                      ...styles.actionBtn,
                      color: '#e74c3c',
                      borderColor: '#e74c3c',
                    }}
                    onClick={() => {
                      if (confirm(`Delete "${d.name}"?`)) {
                        deleteDesign({ designId: d.id });
                        setDesigns((prev) => prev.filter((x) => x.id !== d.id));
                      }
                    }}
                  >
                    Delete
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div style={styles.empty}>
          <span style={{ fontSize: 64 }}>🎨</span>
          <h2 style={{ margin: '16px 0 8px', color: '#333' }}>No designs yet</h2>
          <p style={{ color: '#888', marginBottom: 20 }}>
            Create your first design using the builder.
          </p>
          <a href="/admin/design-builder/editor" style={styles.createBtn}>
            + Create Design
          </a>
        </div>
      )}
    </div>
  );
};

export const DesignListLayout = DesignListLayoutTemplate;

/* ─── Styles ──────────────────────────────────────────────────── */

const styles: Record<string, React.CSSProperties> = {
  root: {
    padding: 24,
    fontFamily: "'Inter', 'Segoe UI', -apple-system, sans-serif",
    maxWidth: 1400,
    margin: '0 auto',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 24,
    flexWrap: 'wrap',
    gap: 12,
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: 12,
  },
  headerRight: {
    display: 'flex',
    alignItems: 'center',
    gap: 10,
  },
  count: {
    backgroundColor: '#e8f0fe',
    color: '#0078d4',
    padding: '4px 10px',
    borderRadius: 12,
    fontSize: 12,
    fontWeight: 600,
  },
  searchInput: {
    padding: '8px 14px',
    border: '1px solid #ddd',
    borderRadius: 6,
    fontSize: 13,
    minWidth: 200,
  },
  createBtn: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: 4,
    backgroundColor: '#0078d4',
    color: '#fff',
    padding: '10px 20px',
    borderRadius: 6,
    fontSize: 14,
    fontWeight: 600,
    textDecoration: 'none',
    cursor: 'pointer',
    border: 'none',
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280, 1fr))',
    gap: 20,
  },
  card: {
    backgroundColor: '#fff',
    borderRadius: 8,
    border: '1px solid #e0e0e0',
    overflow: 'hidden',
    transition: 'box-shadow 0.2s',
  },
  cardPreview: {
    height: 180,
    backgroundColor: '#f5f5f5',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  previewImg: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  placeholder: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: 8,
  },
  cardBody: {
    padding: 14,
  },
  cardTitle: {
    fontSize: 15,
    fontWeight: 600,
    margin: '0 0 4px',
    color: '#333',
  },
  cardMeta: {
    fontSize: 12,
    color: '#999',
    display: 'block',
    marginBottom: 10,
  },
  cardActions: {
    display: 'flex',
    gap: 6,
  },
  editBtn: {
    display: 'inline-block',
    padding: '5px 14px',
    backgroundColor: '#0078d4',
    color: '#fff',
    border: 'none',
    borderRadius: 4,
    fontSize: 12,
    fontWeight: 600,
    textDecoration: 'none',
    cursor: 'pointer',
  },
  actionBtn: {
    padding: '5px 12px',
    backgroundColor: '#fff',
    color: '#555',
    border: '1px solid #ddd',
    borderRadius: 4,
    fontSize: 12,
    cursor: 'pointer',
  },
  empty: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 60,
    textAlign: 'center',
  },
};
