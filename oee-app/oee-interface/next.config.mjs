/** @type {import('next').NextConfig} */
const nextConfig = {
  eslint: {
    // Only ignore warnings during production builds
    ignoreDuringBuilds: false,
    dirs: ['app', 'components', 'lib', 'hooks'],
  },
  typescript: {
    // Fail on TypeScript errors during build
    ignoreBuildErrors: false,
  },
  images: {
    // Keep unoptimized for now - can optimize later if needed
    unoptimized: true,
  },
  // Security headers
  async headers() {
    return [
      {
        source: '/:path*',
        headers: [
          {
            key: 'X-Frame-Options',
            value: 'DENY',
          },
          {
            key: 'X-Content-Type-Options',
            value: 'nosniff',
          },
          {
            key: 'X-XSS-Protection',
            value: '1; mode=block',
          },
        ],
      },
    ];
  },
}

export default nextConfig
