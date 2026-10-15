# Review moderation assistance

Vietnamese dictionary: Blue Eyes, https://github.com/blue-eyes-vn/vietnamese-offensive-words
File: vn_offensive_words.txt, downloaded 2026-10-15, verified against commit
684b568e4d54ce47b743d7c564447e29a02cc260.
SHA256: 9e5fd21803717edfeb579d6e72b56f302008958adb178ffde2f25e021d91cdf6.
License: MIT, copyright 2023 Blue Eyes; original license retained in LICENSE-MIT.txt.
Dictionary retained verbatim. Comment lines are excluded when loading. Ambiguous terms
may trigger false positives: the lexicon is a feature, never an automatic hide rule.

training.tsv contains project-authored illustrative examples (not scraped patient data).
Spam/advertising examples are local seed data, not an externally validated Vietnamese corpus.
The implementation trains a multinomial Naive Bayes model once on process startup,
using word/bigram tokens, dictionary matches, links and repetition, Laplace smoothing
and uniform class priors. Unknown-only and empty text receive the ordinary label.
Top classes above score 60 enter the suggested queue. Scores are normalized model
outputs, not measured accuracy or calibrated probabilities. This small seed model needs
evaluation on administrator-labelled real examples before claiming production accuracy.

No external service receives review text. Suggestions never change visibility or stars.
Admin filters run before pagination across matching reviews. The initial implementation
classifies projected reviews on request; very large datasets should persist versioned
classification results with an offline backfill and classify-on-write job.

Run: dotnet run --project tests/ModerationRegression (from repository root).
