import { useState } from "react";
import { loginWithEmptyBody } from "../services/authService";
import "./styles/LoginPage.css";

export default function LoginPage() {
  const [isLoading, setIsLoading] = useState(false);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

  const handleLogin = () => {
    setIsLoading(true);
    setStatusMessage(null);

    try {
      loginWithEmptyBody();
      setStatusMessage("Redirecting to Google login...");
    } catch (error) {
      const message = error instanceof Error ? error.message : "Login request failed.";
      setIsLoading(false);
      setStatusMessage(message);
    }
  };

  return (
    <section className="login-page">
      <div className="login-page-card">
        <p className="login-page-eyebrow">Trusted Digital Banking</p>
        <h1 className="login-page-title">Welcome to HABO Banking</h1>
        <p className="login-page-subtitle">
          Secure, simple banking for everyday life. Sign in with Google to access your dashboard.
        </p>

        <div className="login-page-features" aria-label="Security features">
          <span className="login-page-feature">Bank-grade security</span>
          <span className="login-page-feature">Fast account overview</span>
          <span className="login-page-feature">Protected Google sign-in</span>
        </div>

        <button className="login-page-cta" onClick={handleLogin} disabled={isLoading}>
          <span className="login-page-google-mark" aria-hidden="true">
            G
          </span>
          {isLoading ? "Redirecting..." : "Login with Google"}
        </button>

        {statusMessage && <p className="login-page-status">{statusMessage}</p>}
      </div>
    </section>
  );
}