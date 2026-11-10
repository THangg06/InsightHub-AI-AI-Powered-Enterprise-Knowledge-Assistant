import { StrictMode, useState } from "react";
import { createRoot } from "react-dom/client";
import "./styles.css";

type Citation = { fileName: string; excerpt: string };
type Answer = { answer: string; citations: Citation[] };

function App() {
  const [workspace, setWorkspace] = useState("demo");
  const [question, setQuestion] = useState("");
  const [answer, setAnswer] = useState<Answer | null>(null);
  const [status, setStatus] = useState("");

  function workspacePath() {
    return encodeURIComponent(workspace.trim());
  }

  async function errorMessage(response: Response) {
    try {
      const body = await response.json() as { error?: string };
      return body.error ?? `Request failed (${response.status}).`;
    } catch {
      return `Request failed (${response.status}).`;
    }
  }

  async function upload(file: File) {
    try {
      const form = new FormData();
      form.append("file", file);
      const response = await fetch(`/api/workspaces/${workspacePath()}/documents`, {
        method: "POST",
        headers: { "X-Workspace-Token": workspace.trim() },
        body: form,
      });
      setStatus(response.ok ? `Uploaded ${file.name}` : await errorMessage(response));
    } catch {
      setStatus("The API is unavailable. Check that the backend is running.");
    }
  }

  async function ask() {
    setStatus("");
    try {
      const response = await fetch(`/api/workspaces/${workspacePath()}/ask`, {
        method: "POST",
        headers: { "Content-Type": "application/json", "X-Workspace-Token": workspace.trim() },
        body: JSON.stringify({ question }),
      });
      if (!response.ok) {
        setStatus(await errorMessage(response));
        setAnswer(null);
        return;
      }
      setAnswer(await response.json() as Answer);
    } catch {
      setStatus("The API is unavailable. Check that the backend is running.");
      setAnswer(null);
    }
  }

  return (
    <main>
      <p className="eyebrow">RAG WORKSPACE</p>
      <h1>Ask your documents.</h1>
      <p className="intro">Upload a text document, then ask a question grounded in its contents.</p>
      <label>Workspace <input value={workspace} onChange={(event) => setWorkspace(event.target.value)} /></label>
      <section className="card">
        <h2>1. Upload</h2>
        <input type="file" accept=".txt,.md,.csv" onChange={(event) => event.target.files?.[0] && upload(event.target.files[0])} />
        {status && <p className="status">{status}</p>}
      </section>
      <section className="card">
        <h2>2. Ask</h2>
        <textarea value={question} onChange={(event) => setQuestion(event.target.value)} placeholder="What does the document explain?" />
        <button disabled={!workspace.trim() || !question.trim()} onClick={ask}>Get cited answer</button>
      </section>
      {status && <p className="status">{status}</p>}
      {answer && <section className="answer"><h2>Answer</h2><p>{answer.answer}</p>{answer.citations.length > 0 && <><h3>Sources</h3><ul>{answer.citations.map((citation, index) => <li key={`${citation.fileName}-${index}`}><strong>{citation.fileName}</strong>: {citation.excerpt}</li>)}</ul></>}</section>}
    </main>
  );
}

createRoot(document.getElementById("root")!).render(<StrictMode><App /></StrictMode>);
