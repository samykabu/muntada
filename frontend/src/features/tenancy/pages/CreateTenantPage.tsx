import { useState, useMemo, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useCreateTenantMutation } from '../api/tenantApi';

const INDUSTRIES = [
  'Education',
  'Healthcare',
  'Technology',
  'Finance',
  'Government',
  'Non-profit',
  'Other',
];

const TEAM_SIZES = ['1-10', '11-50', '51-200', '201-500', '500+'];

/** Converts a name to a URL-safe slug. */
function toSlug(name: string): string {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
    .slice(0, 63);
}

/** Onboarding page for creating a new tenant/organization. */
export function CreateTenantPage() {
  const navigate = useNavigate();
  const [createTenant, { isLoading, error }] = useCreateTenantMutation();

  const [name, setName] = useState('');
  const [slugOverride, setSlugOverride] = useState<string | null>(null);
  const [industry, setIndustry] = useState('');
  const [teamSize, setTeamSize] = useState('');

  const slug = useMemo(() => slugOverride ?? toSlug(name), [name, slugOverride]);

  const nameError = name.length > 0 && (name.length < 3 || name.length > 100)
    ? 'Name must be between 3 and 100 characters'
    : null;

  const slugError = slug.length > 0 && !/^[a-z0-9]([a-z0-9-]*[a-z0-9])?$/.test(slug)
    ? 'Slug must be lowercase letters, numbers, and hyphens only'
    : null;

  const isValid = name.length >= 3 && name.length <= 100 && slug.length > 0 && !slugError;

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!isValid) return;
    try {
      await createTenant({
        name,
        slug,
        industry: industry || undefined,
        teamSize: teamSize || undefined,
      }).unwrap();
      navigate('/');
    } catch {
      // Error is rendered from RTK Query state
    }
  };

  return (
    <div style={{ maxWidth: 480, margin: '2rem auto', padding: '1rem', fontFamily: 'system-ui, sans-serif' }}>
      <h1>Create Your Organization</h1>
      <p style={{ color: '#6b7280', marginBottom: '1.5rem' }}>Set up your team space on Muntada.</p>

      <form onSubmit={handleSubmit}>
        {/* Name */}
        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="tenant-name" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>Organization Name *</label>
          <input
            id="tenant-name"
            type="text"
            value={name}
            onChange={(e) => { setName(e.target.value); if (!slugOverride) setSlugOverride(null); }}
            required
            minLength={3}
            maxLength={100}
            placeholder="Acme Corp"
            style={{ width: '100%' }}
          />
          {nameError && <p style={{ color: 'red', fontSize: '0.8rem', margin: '4px 0 0' }}>{nameError}</p>}
        </div>

        {/* Slug */}
        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="tenant-slug" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>URL Slug *</label>
          <div style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
            <span style={{ color: '#9ca3af', fontSize: '0.875rem' }}>muntada.app/</span>
            <input
              id="tenant-slug"
              type="text"
              value={slug}
              onChange={(e) => setSlugOverride(e.target.value)}
              required
              style={{ flex: 1 }}
            />
          </div>
          {slugError && <p style={{ color: 'red', fontSize: '0.8rem', margin: '4px 0 0' }}>{slugError}</p>}
        </div>

        {/* Industry */}
        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="tenant-industry" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>Industry</label>
          <select id="tenant-industry" value={industry} onChange={(e) => setIndustry(e.target.value)} style={{ width: '100%' }}>
            <option value="">Select (optional)</option>
            {INDUSTRIES.map((i) => <option key={i} value={i}>{i}</option>)}
          </select>
        </div>

        {/* Team size */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label htmlFor="tenant-team-size" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>Team Size</label>
          <select id="tenant-team-size" value={teamSize} onChange={(e) => setTeamSize(e.target.value)} style={{ width: '100%' }}>
            <option value="">Select (optional)</option>
            {TEAM_SIZES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>

        {error && <p role="alert" style={{ color: 'red', marginBottom: '0.75rem' }}>Failed to create organization. Please try again.</p>}

        <button type="submit" disabled={isLoading || !isValid} style={{ width: '100%', padding: '0.5rem' }}>
          {isLoading ? 'Creating...' : 'Create Organization'}
        </button>
      </form>
    </div>
  );
}
