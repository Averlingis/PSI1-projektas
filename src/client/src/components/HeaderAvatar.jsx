import { useState, useEffect, useRef } from "react";
import useProfilePicture from "../hooks/useProfilePicture";
import AvatarImage from "./AvatarImage";
import StreakIcon from "./StreakIcon";

export default function HeaderAvatar({ onEditProfile }) {
  const { imageUrl, loading } = useProfilePicture();
  const [menuOpen, setMenuOpen] = useState(false);
  const containerRef = useRef(null);

  // closes the menu when clicking anywhere else or pressing Escape
  useEffect(() => {
    if (!menuOpen) return;

    function handleClick(event) {
      if (!containerRef.current.contains(event.target)) setMenuOpen(false);
    }

    function handleKey(event) {
      if (event.key === "Escape") setMenuOpen(false);
    }

    document.addEventListener("mousedown", handleClick);
    document.addEventListener("keydown", handleKey);

    return () => {
      document.removeEventListener("mousedown", handleClick);
      document.removeEventListener("keydown", handleKey);
    };
  }, [menuOpen]);

  return (
    <div className="header-avatar" ref={containerRef}>
      <button
        type="button"
        className="header-avatar-button"
        aria-label="Open profile menu"
        aria-haspopup="menu"
        aria-expanded={menuOpen}
        onClick={() => setMenuOpen((open) => !open)}
      >
        <span className="profile-avatar small">
          <AvatarImage imageUrl={imageUrl} loading={loading} />
        </span>
      </button>

      <StreakIcon />

      {menuOpen && (
        <div className="avatar-menu" role="menu">
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setMenuOpen(false);
              onEditProfile();
            }}
          >
            Edit profile
          </button>
        </div>
      )}
    </div>
  );
}