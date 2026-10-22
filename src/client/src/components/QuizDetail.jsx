
export default function QuizDetail({ quiz, onBack, onPlay }) {
  return (
    <div className="quiz-detail">
      <h2>{quiz.title}</h2>

      <p>{quiz.description || "No description available."}</p>

      <div className="quiz-detail-info">
        <p><strong>Category:</strong> {quiz.category}</p>
        <p><strong>Language:</strong> {quiz.language}</p>
        <p><strong>Questions:</strong> {quiz.questionCount}</p>
      </div>

      <div className="step-actions">
        <button className="secondary-button" onClick={onBack}>
          Back
        </button>

        <button onClick={() => onPlay(quiz)}>
          Play
        </button>
      </div>
    </div>
  );
}
