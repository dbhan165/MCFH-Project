import { useId } from 'react';
import { Link } from 'react-router-dom';

type McfhLogoProps = {
  size?: number;
  showText?: boolean;
  subtitle?: string;
  textClassName?: string;
  subtitleClassName?: string;
  className?: string;
  linkTo?: string;
};

export function McfhIcon({ size = 32, className = '' }: { size?: number; className?: string }) {
  const gradId = useId().replace(/:/g, '');
  const shineId = `${gradId}-shine`;

  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 48 48"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      aria-hidden
    >
      <defs>
        <linearGradient id={gradId} x1="6" y1="4" x2="42" y2="44" gradientUnits="userSpaceOnUse">
          <stop stopColor="#FF7575" />
          <stop offset="0.55" stopColor="#FF8F6B" />
          <stop offset="1" stopColor="#00B4D8" />
        </linearGradient>
        <linearGradient id={shineId} x1="24" y1="6" x2="24" y2="42" gradientUnits="userSpaceOnUse">
          <stop stopColor="white" stopOpacity="0.4" />
          <stop offset="1" stopColor="white" stopOpacity="0" />
        </linearGradient>
      </defs>
      <rect x="2" y="2" width="44" height="44" rx="13" fill={`url(#${gradId})`} />
      <rect x="2" y="2" width="44" height="44" rx="13" fill={`url(#${shineId})`} />
      <path
        d="M13 33V15l9.5 12L32 15v18"
        stroke="white"
        strokeWidth="3.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M34 28c0-3.5 2.5-6 5.5-6"
        stroke="white"
        strokeWidth="2"
        strokeLinecap="round"
        strokeOpacity="0.85"
      />
      <circle cx="36" cy="13" r="3" fill="white" />
      <circle cx="36" cy="13" r="5.5" stroke="white" strokeWidth="1.2" strokeOpacity="0.4" fill="none" />
    </svg>
  );
}

export default function McfhLogo({
  size = 32,
  showText = true,
  subtitle,
  textClassName = 'text-white text-xl',
  subtitleClassName = 'text-xs text-gray-500',
  className = '',
  linkTo,
}: McfhLogoProps) {
  const content = (
    <div className={`flex items-center gap-2.5 ${className}`}>
      <McfhIcon size={size} className="shrink-0 drop-shadow-[0_4px_12px_rgba(255,117,117,0.25)]" />
      {showText && (
        <div className="flex flex-col leading-none">
          <span className={`font-extrabold tracking-[0.18em] ${textClassName}`}>MCFH</span>
          {subtitle ? <span className={`mt-1 font-medium tracking-wide ${subtitleClassName}`}>{subtitle}</span> : null}
        </div>
      )}
    </div>
  );

  if (linkTo) {
    return (
      <Link to={linkTo} className="inline-flex hover:opacity-90 transition-opacity">
        {content}
      </Link>
    );
  }

  return content;
}
