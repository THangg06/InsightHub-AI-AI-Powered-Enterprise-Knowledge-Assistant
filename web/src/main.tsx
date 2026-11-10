import { StrictMode, useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import "./styles.css";

type User = { id: string; email: string };
type Workspace = { id: string; name: string };
type Citation = { documentId: string; fileName: string; excerpt: string };
type Answer = { answer: string; citations: Citation[] };
type Document = { id: string; fileName: string; contentType: string; processingStatus: string; uploadedAt: string };
type Chat = { id: string; title: string; createdAt: string };

async function request<T>(url: string, options: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem("token");
  const headers = new Headers(options.headers);
  if (token) headers.set("Authorization", `Bearer ${token}`);
  const response = await fetch(url, { ...options, headers });
  if (!response.ok) {
    const body = await response.json().catch(() => ({})) as { error?: string };
    throw new Error(body.error ?? `Request failed (${response.status})`);
  }
  return response.status === 204 ? undefined as T : await response.json() as T;
}

function App() {
  const [user, setUser] = useState<User | null>(null);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [workspaceName, setWorkspaceName] = useState("My workspace");
  const [workspace, setWorkspace] = useState<Workspace | null>(null);
  const [documents, setDocuments] = useState<Document[]>([]);
  const [question, setQuestion] = useState("");
  const [answer, setAnswer] = useState<Answer | null>(null);
  const [chats, setChats] = useState<Chat[]>([]);
  const [status, setStatus] = useState("");

  useEffect(() => {
    if (localStorage.getItem("token")) request<User>("/api/auth/me").then(setUser).catch(() => localStorage.removeItem("token"));
  }, []);

  async function authenticate(path: "/api/auth/login" | "/api/auth/register") {
    try {
      const result = await request<{ token: string; userId: string; email: string }>(path, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email, password }) });
      localStorage.setItem("token", result.token);
      setUser({ id: result.userId, email: result.email });
      setStatus("Authenticated.");
    } catch (error) { setStatus(error instanceof Error ? error.message : "Authentication failed."); }
  }

  async function createWorkspace() {
    try {
      const created = await request<Workspace>("/api/workspaces", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: workspaceName }) });
      setWorkspace(created);
      setStatus("Workspace created.");
    } catch (error) { setStatus(error instanceof Error ? error.message : "Workspace creation failed."); }
  }

  async function loadDocuments(id: string) {
    setDocuments(await request<Document[]>(`/api/workspaces/${id}/documents`));
  }

  async function loadChats(id: string) {
    setChats(await request<Chat[]>(`/api/workspaces/${id}/chats`));
  }

  async function deleteChat(id: string) {
    try {
      await request(`/api/chats/${id}`, { method: "DELETE" });
      if (workspace) await loadChats(workspace.id);
      setAnswer(null);
      setStatus("Chat deleted.");
    } catch (error) { setStatus(error instanceof Error ? error.message : "Chat deletion failed."); }
  }

  async function upload(file: File) {
    if (!workspace) return;
    try {
      const form = new FormData(); form.append("file", file);
      await request(`/api/workspaces/${workspace.id}/documents`, { method: "POST", body: form });
      await loadDocuments(workspace.id); setStatus(`Uploaded ${file.name}`);
    } catch (error) { setStatus(error instanceof Error ? error.message : "Upload failed."); }
  }

  async function ask() {
    if (!workspace) return;
    try {
      setAnswer(await request<Answer>(`/api/workspaces/${workspace.id}/ask`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ question }) }));
      await loadChats(workspace.id);
    } catch (error) { setStatus(error instanceof Error ? error.message : "Question failed."); }
  }

  if (!user) return <main><p className="eyebrow">RAG WORKSPACE</p><h1>Secure document answers.</h1><p className="intro">Register or sign in to create an isolated workspace.</p><section className="card"><label>Email<input type="email" value={email} onChange={(event) => setEmail(event.target.value)} /></label><label>Password<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} /></label><button onClick={() => authenticate("/api/auth/register")}>Register</button><button className="secondary" onClick={() => authenticate("/api/auth/login")}>Sign in</button>{status && <p className="status">{status}</p>}</section></main>;

  return <main><p className="eyebrow">RAG WORKSPACE</p><h1>Ask your documents.</h1><p className="intro">Signed in as {user.email}</p><section className="card"><h2>Workspace</h2>{workspace ? <><strong>{workspace.name}</strong><button className="secondary" onClick={() => { void loadDocuments(workspace.id); void loadChats(workspace.id); }}>Refresh workspace</button></> : <><input value={workspaceName} onChange={(event) => setWorkspaceName(event.target.value)} /><button onClick={createWorkspace}>Create workspace</button></>}</section>{workspace && <><section className="card"><h2>Chat history</h2>{chats.length === 0 ? <p>No conversations yet.</p> : <ul>{chats.map((chat) => <li key={chat.id}><span>{chat.title}</span><button className="secondary" onClick={() => deleteChat(chat.id)}>Delete</button></li>)}</ul>}</section><section className="card"><h2>Upload document</h2><input type="file" accept=".pdf,.docx,.txt,.md,.csv" onChange={(event) => event.target.files?.[0] && upload(event.target.files[0])} /><ul>{documents.map((document) => <li key={document.id}>{document.fileName} — {document.processingStatus}</li>)}</ul></section><section className="card"><h2>Ask</h2><textarea value={question} onChange={(event) => setQuestion(event.target.value)} placeholder="What does the document explain?" /><button disabled={!question.trim()} onClick={ask}>Get cited answer</button></section>{answer && <section className="answer"><h2>Answer</h2><p>{answer.answer}</p><h3>Sources</h3><ul>{answer.citations.map((citation) => <li key={citation.documentId}><strong>{citation.fileName}</strong>: {citation.excerpt}</li>)}</ul></section>}</>}{status && <p className="status">{status}</p>}</main>;
}

createRoot(document.getElementById("root")!).render(<StrictMode><App /></StrictMode>);
