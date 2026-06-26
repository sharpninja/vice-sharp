# AI reviews

This directory holds the saved output of the aiUnit AI reviews
(`tests/ViceSharp.AiReview.Tests`). Each review run writes one timestamped
markdown file here containing the prompt sent to the model and the model's
response (the `aiunit.review.findings.v1` JSON, pretty-printed):

```
{kind}-review-{yyyyMMddTHHmmssfffZ}.md   e.g. code-review-20260621T070400123Z.md
```

`kind` is `code` or `project`. Files sort chronologically by name. Commit the
review markdown you want to keep as a record; they are written only when the
reviews are run on demand (see [../AI-Review.md](../AI-Review.md)).
