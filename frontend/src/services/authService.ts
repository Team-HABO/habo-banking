import axios from 'axios';

const authApiUrl = import.meta.env.VITE_AUTH_API_URL;

/**
 * Initiates the OAuth login flow by submitting a hidden HTML form.
 * * @remarks
 * This function specifically avoids using Axios/Fetch (XHR) because the 
 * ASP.NET backend issues a `ChallengeResult`. This triggers a 302 redirect 
 * to Google's OAuth servers, which would be blocked by CORS if attempted 
 * via a standard AJAX request. A full browser navigation via form submission 
 * allows the redirect and cookie-setting to happen naturally.
 * * @throws {Error} If the `VITE_AUTH_API_URL` environment variable is not defined.
 */
export function login() {
	if (!authApiUrl) {
		throw new Error("Missing VITE_AUTH_API_URL in environment variables.");
	}

	const form = document.createElement("form");
	form.method = "POST";
	form.action = `${authApiUrl}/api/auth/tokens`;
	form.style.display = "none";

	document.body.appendChild(form);
	form.submit();
}

/**
 * Logs the user out by calling the backend API to clear the auth cookie.
 * @remarks
 * automatically handles the cookie deletion based on the server response.
 */
export async function logout() {
    if (!authApiUrl) {
        throw new Error("Missing VITE_AUTH_API_URL in environment variables.");
    }

    try {
        await axios.post(`${authApiUrl}/api/auth/logout`, {}, {
            withCredentials: true // Required to send/receive cookies
        });
        
        // Used to ensure the browser completely clears out any sensitive in-memory data
        window.location.href = '/';
    } catch (error) {
        console.error("Logout failed:", error);
    }
}
