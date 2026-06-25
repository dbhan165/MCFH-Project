import type { ProjectInfluencer } from '../types/project';

const SOV_COLORS = ['#00B4D8', '#FF7575', '#EAB308', '#A78BFA', '#34D399', '#64748B'];

export function buildSovSlices(influencers: ProjectInfluencer[], topN = 5) {
  const sorted = [...influencers].sort((a, b) => b.shareOfVoice - a.shareOfVoice);
  const top = sorted.slice(0, topN);
  const restSov = sorted.slice(topN).reduce((sum, i) => sum + i.shareOfVoice, 0);

  const slices = top.map((kol, idx) => ({
    name: kol.name,
    value: kol.shareOfVoice,
    mentions: kol.mentions,
    color: SOV_COLORS[idx % SOV_COLORS.length],
    platform: kol.platform,
  }));

  if (restSov > 0) {
    slices.push({
      name: 'Khác',
      value: Math.round(restSov * 10) / 10,
      mentions: sorted.slice(topN).reduce((s, i) => s + i.mentions, 0),
      color: SOV_COLORS[5],
      platform: 'mixed',
    });
  }

  return slices;
}

export function getAvatarInitials(name: string): string {
  const clean = name.replace(/^@/, '').trim();
  if (!clean) return '?';
  const parts = clean.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
  return clean.slice(0, 2).toUpperCase();
}

export function getAvatarColor(seed: string): string {
  let hash = 0;
  for (let i = 0; i < seed.length; i++) hash = seed.charCodeAt(i) + ((hash << 5) - hash);
  const hue = Math.abs(hash) % 360;
  return `hsl(${hue} 65% 45%)`;
}

export function formatFollowers(count: number | null): string {
  if (count == null || count <= 0) return '—';
  if (count >= 1_000_000) return `${(count / 1_000_000).toFixed(1)}M`;
  if (count >= 1_000) return `${(count / 1_000).toFixed(1)}K`;
  return String(count);
}
