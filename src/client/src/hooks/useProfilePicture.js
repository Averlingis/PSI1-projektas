import { useState, useEffect, useLayoutEffect } from "react";
import { getProfilePicture } from "../api";

// loads the user's profile picture and turns it into a URL an <img> can show
// initialPicture: undefined = load it here, null = preloaded and there's no picture, Blob = preloaded picture
export default function useProfilePicture(initialPicture) {
  const preloaded = initialPicture !== undefined;
  // the picture data: a Blob, null when there's no picture, undefined while it's not known yet
  const [picture, setPicture] = useState(initialPicture);
  const [imageUrl, setImageUrl] = useState(null);
  const [error, setError] = useState("");
  const [reloadKey, setReloadKey] = useState(0);

  // load the saved picture from the backend
  useEffect(() => {
    // a preloaded picture is already known, so it's only loaded again after an upload
    if (preloaded && reloadKey === 0) return;

    let cancelled = false;

    getProfilePicture()
      .then((blob) => {
        if (!cancelled) setPicture(blob);
      })
      .catch((err) => {
        if (cancelled) return;
        setError(err.message);
        setPicture(null);
      });

    return () => {
      cancelled = true;
    };
  }, [reloadKey, preloaded]);

  // makes a URL for the <img> from the picture, and frees it when it's no longer needed
  // making and freeing it in the same place means the picture can never be left with a freed URL
  useLayoutEffect(() => {
    if (!picture) {
      setImageUrl(null);
      return;
    }

    const url = URL.createObjectURL(picture);
    setImageUrl(url);

    return () => {
      URL.revokeObjectURL(url);
    };
  }, [picture]);

  // loads the picture again after a new one was uploaded
  function reload() {
    setReloadKey((k) => k + 1);
  }

  return {
    imageUrl,
    // still loading only while it's not known yet whether there's a picture
    loading: picture === undefined,
    error,
    reload,
  };
}