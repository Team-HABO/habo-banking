const authApiUrl = import.meta.env.VITE_AUTH_API_URL;

export function loginWithEmptyBody() {
	if (!authApiUrl) {
		throw new Error("Missing VITE_AUTH_API_URL in environment variables.");
	}

	// OAuth login endpoints should be initiated by browser navigation, not XHR.
	const form = document.createElement("form");
	form.method = "POST";
	form.action = `${authApiUrl}/api/auth/tokens`;
	form.style.display = "none";

	document.body.appendChild(form);
	form.submit();
}
