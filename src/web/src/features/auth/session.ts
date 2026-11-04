// Remembers that we were signed out by an expired cookie (not by choice),
// so the sign-in page can say so. Lives in memory: a full reload forgets it, which is fine.
let expired = false

export const markSessionExpired = () => { expired = true }
export const clearSessionExpired = () => { expired = false }
export const isSessionExpired = () => expired
