# Interview demonstration (5–7 minutes)

## Simple English script

“This is RAG Workspace, a full-stack document question-answering demonstration. The React client authenticates users, creates workspaces, uploads documents, and asks questions. The ASP.NET Core API validates files, isolates workspaces, extracts text, chunks content, retrieves relevant evidence, and returns citations.

The local stack uses EF Core with an in-memory development provider or PostgreSQL/pgvector in Docker. It uses deterministic embeddings for reproducible local tests and supports optional Gemini synthesis when a key is configured. The system explicitly handles unsupported or missing evidence.

I can demonstrate the flow: enter a workspace, upload a text file, ask a question, and inspect the cited source. The tests cover matching citations and irrelevant questions. GitHub Actions restores and tests the .NET solution and builds the React application. Deployment to Azure is intentionally postponed until the local and CI paths are stable.

AI helped analyze requirements, draft architecture, generate test cases, review edge cases, and produce documentation. I reviewed every suggestion and verified behavior with tests. Current limitations are in-memory data, simple retrieval, and demo authorization. Future improvements are semantic retrieval, durable storage, authentication, observability, and Docker validation.”

## Vietnamese explanation

“Đây là RAG Workspace, một ứng dụng full-stack có đăng nhập, workspace, upload tài liệu và hỏi đáp có trích dẫn. React xử lý giao diện; ASP.NET Core kiểm tra file, trích xuất text, chia chunk, tìm nội dung liên quan và trả về nguồn trích dẫn.

Local stack dùng EF Core InMemory hoặc PostgreSQL/pgvector trong Docker. Embedding deterministic giúp test ổn định; Gemini là tùy chọn khi cấu hình API key. Hệ thống từ chối rõ ràng khi không có bằng chứng phù hợp.

Tôi có thể demo: nhập workspace, upload file text, đặt câu hỏi và kiểm tra citation. Test kiểm tra kết quả có nguồn và trường hợp không liên quan. GitHub Actions restore, build, test backend và build frontend. Azure deployment sẽ chỉ thêm sau khi local và CI ổn định.”
