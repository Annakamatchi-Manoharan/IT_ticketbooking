import "./globals.css";
export const metadata = { title: "Enterprise IT Ticket System" };
export default function RootLayout({ children }) { return (<html lang="en"><body><div className="min-h-screen"><header className="border-b border-slate-300 dark:border-slate-700 p-4 font-semibold">IT Ticket System</header><main className="p-6">{children}</main></div></body></html>); }
