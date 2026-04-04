import { useState, useCallback, type FormEvent, type DragEvent, type ChangeEvent } from 'react';
import { useUpdateBrandingMutation } from '../api/tenantApi';

interface BrandingEditorProps {
  tenantId: string;
  initialLogoUrl?: string;
  initialPrimaryColor?: string;
  initialSecondaryColor?: string;
  initialCustomDomain?: string;
}

const MAX_LOGO_SIZE = 5 * 1024 * 1024; // 5 MB

/** Editor for tenant branding: logo, colors, and custom domain. */
export function BrandingEditor({
  tenantId,
  initialLogoUrl,
  initialPrimaryColor = '#3b82f6',
  initialSecondaryColor = '#64748b',
  initialCustomDomain = '',
}: BrandingEditorProps) {
  const [logoFile, setLogoFile] = useState<File | null>(null);
  const [logoPreview, setLogoPreview] = useState<string | undefined>(initialLogoUrl);
  const [primaryColor, setPrimaryColor] = useState(initialPrimaryColor);
  const [secondaryColor, setSecondaryColor] = useState(initialSecondaryColor);
  const [customDomain, setCustomDomain] = useState(initialCustomDomain);
  const [fileError, setFileError] = useState<string | null>(null);
  const [isDragOver, setIsDragOver] = useState(false);

  const [updateBranding, { isLoading, isSuccess, error }] = useUpdateBrandingMutation();

  const handleFile = useCallback((file: File) => {
    setFileError(null);
    if (file.size > MAX_LOGO_SIZE) {
      setFileError('Logo must be under 5 MB');
      return;
    }
    if (!file.type.startsWith('image/')) {
      setFileError('File must be an image');
      return;
    }
    setLogoFile(file);
    setLogoPreview(URL.createObjectURL(file));
  }, []);

  const onDrop = useCallback((e: DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
    const file = e.dataTransfer.files[0];
    if (file) handleFile(file);
  }, [handleFile]);

  const onFileInput = useCallback((e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFile(file);
  }, [handleFile]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const formData = new FormData();
    if (logoFile) formData.append('logo', logoFile);
    formData.append('primaryColor', primaryColor);
    formData.append('secondaryColor', secondaryColor);
    formData.append('customDomain', customDomain);
    await updateBranding({ tenantId, formData });
  };

  return (
    <form onSubmit={handleSubmit} style={{ maxWidth: 480 }}>
      <h3>Logo</h3>
      <div
        onDrop={onDrop}
        onDragOver={(e) => { e.preventDefault(); setIsDragOver(true); }}
        onDragLeave={() => setIsDragOver(false)}
        style={{
          border: `2px dashed ${isDragOver ? '#3b82f6' : '#d1d5db'}`,
          borderRadius: 8,
          padding: '1.5rem',
          textAlign: 'center',
          cursor: 'pointer',
          backgroundColor: isDragOver ? '#eff6ff' : undefined,
          marginBottom: '0.5rem',
        }}
      >
        {logoPreview ? (
          <img src={logoPreview} alt="Logo preview" style={{ maxHeight: 80, maxWidth: '100%' }} />
        ) : (
          <p>Drag & drop an image here, or click to select</p>
        )}
        <input type="file" accept="image/*" onChange={onFileInput} style={{ display: 'none' }} id="logo-upload" />
        <label htmlFor="logo-upload" style={{ cursor: 'pointer', color: '#3b82f6', textDecoration: 'underline' }}>
          Choose file
        </label>
      </div>
      {fileError && <p role="alert" style={{ color: 'red', fontSize: '0.875rem' }}>{fileError}</p>}

      <h3>Colors</h3>
      <div style={{ display: 'flex', gap: '1rem', marginBottom: '1rem' }}>
        <div>
          <label htmlFor="primary-color" style={{ display: 'block', fontSize: '0.875rem', marginBottom: 4 }}>Primary</label>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <input id="primary-color" type="text" value={primaryColor} onChange={(e) => setPrimaryColor(e.target.value)} style={{ width: 90 }} />
            <div style={{ width: 28, height: 28, backgroundColor: primaryColor, borderRadius: 4, border: '1px solid #ccc' }} />
          </div>
        </div>
        <div>
          <label htmlFor="secondary-color" style={{ display: 'block', fontSize: '0.875rem', marginBottom: 4 }}>Secondary</label>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <input id="secondary-color" type="text" value={secondaryColor} onChange={(e) => setSecondaryColor(e.target.value)} style={{ width: 90 }} />
            <div style={{ width: 28, height: 28, backgroundColor: secondaryColor, borderRadius: 4, border: '1px solid #ccc' }} />
          </div>
        </div>
      </div>

      <h3>Custom Domain</h3>
      <input
        id="custom-domain"
        type="text"
        value={customDomain}
        onChange={(e) => setCustomDomain(e.target.value)}
        placeholder="e.g. community.example.com"
        style={{ width: '100%', marginBottom: '1rem' }}
      />

      {error && <p role="alert" style={{ color: 'red' }}>Failed to save branding. Please try again.</p>}
      {isSuccess && <p style={{ color: 'green' }}>Branding saved successfully.</p>}

      <button type="submit" disabled={isLoading}>
        {isLoading ? 'Saving...' : 'Save Branding'}
      </button>
    </form>
  );
}
