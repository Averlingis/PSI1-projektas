export default function AvatarImage({ imageUrl, loading = false }) {
  if (imageUrl) {
    return <img src={imageUrl} alt="Your profile picture" />;
  }

  // nothing yet, so the placeholder doesn't flash before the real picture arrives
  if (loading) return null;

  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <circle cx="12" cy="8" r="4" />
      <path d="M4 21c0-4.4 3.6-8 8-8s8 3.6 8 8z" />
    </svg>
  );
}