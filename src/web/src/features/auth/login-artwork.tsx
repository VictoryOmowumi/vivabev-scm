/** A bottling plant at dusk, drawn in SVG. Swap for a real VivaBev photo when there is one. */
export function LoginArtwork() {
  return (
    <svg aria-hidden="true" viewBox="0 0 700 900" preserveAspectRatio="xMidYMid slice" className="absolute inset-0 size-full">
      <defs>
        <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#16362A" />
          <stop offset="0.45" stopColor="#2E5A43" />
          <stop offset="0.68" stopColor="#B98A4E" />
          <stop offset="0.78" stopColor="#E9B871" />
        </linearGradient>
        <linearGradient id="glow" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#FFE2A8" />
          <stop offset="1" stopColor="#F0A54A" />
        </linearGradient>
        <linearGradient id="roof" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0" stopColor="#2A2F2A" />
          <stop offset="1" stopColor="#151915" />
        </linearGradient>
        <linearGradient id="water" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#3B3424" />
          <stop offset="1" stopColor="#0C1A12" />
        </linearGradient>
        <linearGradient id="shade" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0.45" stopColor="#000" stopOpacity="0" />
          <stop offset="1" stopColor="#05110A" stopOpacity="0.85" />
        </linearGradient>
        <linearGradient id="bottle" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0" stopColor="#1D4A2C" />
          <stop offset="0.35" stopColor="#4E9A5C" />
          <stop offset="1" stopColor="#16391F" />
        </linearGradient>
      </defs>
      <rect width="700" height="900" fill="url(#sky)" />
      <circle cx="560" cy="470" r="90" fill="#FFD996" opacity="0.35" />
      <path d="M0 560 C120 520 220 545 330 530 S560 500 700 535 V640 H0Z" fill="#24402F" opacity="0.85" />
      <path d="M40 470 C160 400 300 470 420 430 S620 380 700 420 V445 C620 410 520 460 420 458 S170 425 40 495Z" fill="url(#roof)" />
      <rect x="70" y="470" width="600" height="150" fill="#1B201B" />
      <g fill="url(#glow)">
        <rect x="86" y="486" width="34" height="122" opacity="0.95" /><rect x="128" y="480" width="34" height="128" opacity="0.85" />
        <rect x="170" y="476" width="34" height="132" opacity="0.95" /><rect x="212" y="476" width="34" height="132" opacity="0.7" />
        <rect x="254" y="476" width="34" height="132" opacity="0.95" /><rect x="296" y="472" width="34" height="136" opacity="0.9" />
        <rect x="338" y="466" width="34" height="142" opacity="0.8" /><rect x="380" y="462" width="34" height="146" opacity="0.95" />
        <rect x="422" y="460" width="34" height="148" opacity="0.9" /><rect x="464" y="456" width="34" height="152" opacity="0.75" />
        <rect x="506" y="450" width="34" height="158" opacity="0.95" /><rect x="548" y="446" width="34" height="162" opacity="0.85" />
        <rect x="590" y="442" width="34" height="166" opacity="0.95" /><rect x="632" y="440" width="30" height="168" opacity="0.8" />
      </g>
      <g fill="#20261F"><rect x="600" y="340" width="40" height="130" rx="20" /><rect x="648" y="360" width="34" height="110" rx="17" /></g>
      <rect x="0" y="618" width="700" height="14" fill="#2B2A22" />
      <rect x="0" y="632" width="700" height="268" fill="url(#water)" />
      <g fill="#F2B866" opacity="0.28">
        <rect x="90" y="640" width="30" height="80" /><rect x="174" y="640" width="30" height="110" /><rect x="258" y="640" width="30" height="90" />
        <rect x="384" y="640" width="30" height="120" /><rect x="510" y="640" width="30" height="100" /><rect x="594" y="640" width="30" height="130" />
      </g>
      <rect x="0" y="790" width="700" height="10" fill="#0A140D" />
      <g fill="url(#bottle)" opacity="0.95">
        <path d="M60 790 v-62 c0-14 10-20 10-34 v-14 h12 v14 c0 14 10 20 10 34 v62Z" />
        <path d="M140 790 v-62 c0-14 10-20 10-34 v-14 h12 v14 c0 14 10 20 10 34 v62Z" />
        <path d="M220 790 v-62 c0-14 10-20 10-34 v-14 h12 v14 c0 14 10 20 10 34 v62Z" />
        <path d="M300 790 v-62 c0-14 10-20 10-34 v-14 h12 v14 c0 14 10 20 10 34 v62Z" />
        <path d="M380 790 v-62 c0-14 10-20 10-34 v-14 h12 v14 c0 14 10 20 10 34 v62Z" />
        <path d="M460 790 v-62 c0-14 10-20 10-34 v-14 h12 v14 c0 14 10 20 10 34 v62Z" />
        <path d="M540 790 v-62 c0-14 10-20 10-34 v-14 h12 v14 c0 14 10 20 10 34 v62Z" />
        <path d="M620 790 v-62 c0-14 10-20 10-34 v-14 h12 v14 c0 14 10 20 10 34 v62Z" />
      </g>
      <g fill="#FFE2A8" opacity="0.5">
        <rect x="66" y="730" width="3" height="50" rx="1.5" /><rect x="146" y="730" width="3" height="50" rx="1.5" /><rect x="226" y="730" width="3" height="50" rx="1.5" /><rect x="306" y="730" width="3" height="50" rx="1.5" />
        <rect x="386" y="730" width="3" height="50" rx="1.5" /><rect x="466" y="730" width="3" height="50" rx="1.5" /><rect x="546" y="730" width="3" height="50" rx="1.5" /><rect x="626" y="730" width="3" height="50" rx="1.5" />
      </g>
      <rect width="700" height="900" fill="url(#shade)" />
    </svg>
  )
}
