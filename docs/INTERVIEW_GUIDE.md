# Interview demonstration (5–7 minutes)

## Simple English script

“This is RAG Workspace, a full-stack document question-answering demonstration. The React client uploads text documents and asks questions. The ASP.NET Core API validates files, isolates workspaces, retrieves relevant document excerpts, and returns citations.

I chose a provider-free baseline so the project is reproducible in local development and CI. The current retrieval ranks documents by keyword overlap; it explicitly says when no document supports an answer. In production I would add persistent storage, chunking, embeddings, vector search, authentication, and an LLM gateway with timeouts.

I can demonstrate the flow: enter a workspace, upload a text file, ask a question, and inspect the cited source. The tests cover matching citations and irrelevant questions. GitHub Actions restores and tests the .NET solution and builds the React application. Deployment to Azure is intentionally postponed until the local and CI paths are stable.

AI helped analyze requirements, draft architecture, generate test cases, review edge cases, and produce documentation. I reviewed every suggestion and verified behavior with tests. Current limitations are in-memory data, simple retrieval, and demo authorization. Future improvements are semantic retrieval, durable storage, authentication, observability, and Docker validation.”

## Vietnamese explanation

“Đây là RAG Workspace, một ứng dụng full-stack cho phép tải tài liệu lên và hỏi đáp có trích dẫn. React xử lý giao diện; ASP.NET Core kiểm tra file, tách workspace, tìm các tài liệu liên quan và trả về nguồn trích dẫn.

Phiên bản hiện tại không phụ thuộc API bên ngoài để dễ chạy local và CI. Bộ tìm kiếm dùng mức độ trùng khớp từ khóa và thông báo rõ khi không tìm thấy câu trả lời. Khi triển khai thực tế, có thể bổ sung cơ sở dữ liệu, chia nhỏ tài liệu, embedding, vector search, xác thực và LLM gateway có timeout.

Tôi có thể demo: nhập workspace, upload file text, đặt câu hỏi và kiểm tra citation. Test kiểm tra kết quả có nguồn và trường hợp không liên quan. GitHub Actions restore, build, test backend và build frontend. Azure deployment sẽ chỉ thêm sau khi local và CI ổn định.”

