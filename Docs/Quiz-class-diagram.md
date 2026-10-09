<!-- Made with AI> -->

# Quiz class diagram

```mermaid
classDiagram
  class Quiz {
    +int Id
    +string Title
    +string? Description
    +Category Category
    +Language Language
    +IReadOnlyList~Question~ Questions
  }
  class Question {
    +int Id
    +string QuestionText
    +int Position
    +IReadOnlyList~AnswerOption~ Options
  }
  class AnswerOption {
    <<readonly struct>>
    +string OptionText
    +bool IsCorrect
  }
  class AnswerOptionRow {
    <<db row>>
    +int Id
    +string OptionText
    +bool IsCorrect
  }
  class QuizScore {
    +int Id
    +int UserId
    +int QuizId
    +IReadOnlyDictionary~int,bool~ Answers
    +RecordAnswer(questionIndex, isCorrect)
  }
  class QuestionAnswer {
    <<db row>>
    +int Id
    +int QuizScoreId
    +int QuestionIndex
    +bool IsCorrect
  }
  class User {
    +int Id
    +string Email
    +string Username
  }
  class Category {
    <<enumeration>>
  }
  class Language {
    <<enumeration>>
  }
  Quiz "1" *-- "3..100" Question
  Question "1" *-- "2..8" AnswerOptionRow
  Question ..> AnswerOption : Options built from rows
  QuizScore "1" *-- "0..*" QuestionAnswer
  QuizScore "*" --> "1" Quiz
  QuizScore "*" --> "1" User
  Quiz --> Category
  Quiz --> Language
```

`AnswerOptionRow` and `QuestionAnswer` are database rows. EF Core cannot store a struct collection or a dictionary directly, so `Question.Options` and `QuizScore.Answers` are built from these rows.