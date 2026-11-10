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

  async function upload(file: File) {
    const form = new FormData();
    form.append("file", file);
    const response = await fetch(`/api/workspaces/${workspace}/documents`, {
      method: "POST",
      headers: { "X-Workspace-Token": workspace },
      body: form,
    });
    setStatus(response.ok ? `Uploaded ${file.name}` : (await response.json()).error);
  }

  async function ask() {
    const response = await fetch(`/api/workspaces/${workspace}/ask`, {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-Workspace-Token": workspace },
      body: JSON.stringify({ question }),
    });
    setAnswer(await response.json());
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
        <button disabled={!question.trim()} onClick={ask}>Get cited answer</button>
      </section>
      {answer && <section className="answer"><h2>Answer</h2><p>{answer.answer}</p>{answer.citations.length > 0 && <><h3>Sources</h3><ul>{answer.citations.map((citation) => <li key={citation.fileName}><strong>{citation.fileName}</strong>: {citation.excerpt}</li>)}</ul></>}</section>}
    </main>
  );
}

createRoot(document.getElementById("root")!).render(<StrictMode><App /></StrictMode>);

