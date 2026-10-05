import { useState } from "react";
import { register } from "../api";

export default function RegisterForm({ onSuccess, onSwitchToLogin }) {
  const [email, setEmail] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");

    if (!email || !username || !password) {
      setError("Please fill out all fields.");
      return;
    }

    setLoading(true);

    try {
      await register(email, username, password);
      onSuccess();
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <form className="auth-form" onSubmit={handleSubmit} noValidate>
      <h2>Create your account</h2>

      <label>
        Email
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
      </label>

      <label>
        Username
        <input
          type="text"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
        />
      </label>

      <label>
        Password
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
      </label>

      {error && <p className="auth-error">{error}</p>}

      <button type="submit" disabled={loading}>
        {loading ? "Creating account..." : "Create account"}
      </button>

      <p className="auth-switch">
        Already have an account?{" "}
        <button type="button" className="link-button" onClick={onSwitchToLogin}>
          Log in
        </button>
      </p>
    </form>
  );
}