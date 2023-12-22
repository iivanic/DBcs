do $$
    begin
        if (not exists(Select * from public.categories)) then
            INSERT INTO public.categories(name, is_deleted)
            VALUES ('General knowledge', false);
            INSERT INTO public.categories(name, is_deleted)
            VALUES ('Music', false);
            INSERT INTO public.categories(name, is_deleted)
            VALUES ('Science', false);
            INSERT INTO public.categories(name, is_deleted)
            VALUES ('Technology', false);
        end if;
        if (not exists(Select * from public.authors)) then
            INSERT INTO public.authors(name, is_deleted)
            VALUES ('Quiz Master', false);
            INSERT INTO public.authors(name, is_deleted)
            VALUES ('Teacher', false);
        end if;

        if (not exists(Select * from public.questions)) then
            INSERT INTO public.questions(difficulty, category_id, author_id, question, correct_answer, incorrect_answers, is_deleted)
            VALUES (0, 4, 1, 'Which programming languague is the best?', 'C#', '{"C","C++","COBOL"}', false);
            INSERT INTO public.questions(difficulty, category_id, author_id, question, correct_answer, incorrect_answers, is_deleted)
            VALUES (1, 2, 2, 'First George Michael Album?', 'Faith', '{"Older","Listen Without Prejudice Vol. 1","Patience"}', false);
            INSERT INTO public.questions(difficulty, category_id, author_id,  question, correct_answer, incorrect_answers, is_deleted)
            VALUES (2, 1, 1, 'Microsoft Xbox?', 'Super', '{"Good","Bad","Ok"}', false);
            INSERT INTO public.questions(difficulty, category_id, author_id, question, correct_answer, incorrect_answers, is_deleted)
            VALUES (2, 1, 2, 'Playstation 5?', 'Extra', '{"Good","Bad","Ok"}', false);
            INSERT INTO public.questions(difficulty, category_id, author_id, question, correct_answer, incorrect_answers, is_deleted)
            VALUES (0, 1, 1,'James Webb is ...', 'Space Telescope', '{"Ground Telescope","Mega Telescope","Hubble"}', false);
            INSERT INTO public.questions(difficulty, category_id, author_id, question, correct_answer, incorrect_answers, is_deleted)
            VALUES (0, 3, 2, 'Formula e=mc^2 is called:', 'Mass-energy equivalence', '{"Einsteins theory","Proof that we live in multiverse","Special relativity"}', false);
        end if;

        if (not exists(Select * from public.quizzes)) then
                    INSERT INTO public.quizzes(name, avalaible_from, avalaibilty_end, author_id) values('Midnight Quiz',now(), null,1);
                    INSERT INTO public.quizzes(name, avalaible_from, avalaibilty_end, author_id) values('Daily challenge',now(), now() + interval '1' day,2 );
        end if;

        if (not exists(Select * from public.quizzes_questions)) then
                    INSERT INTO public.quizzes_questions(quiz_id, question_id ) 
                    values(1,1);
                    INSERT INTO public.quizzes_questions(quiz_id, question_id) 
                    values(1,2);
                    INSERT INTO public.quizzes_questions(quiz_id, question_id )
                    values(1,3);
                    INSERT INTO public.quizzes_questions(quiz_id, question_id )
                    values(1,4);
                    INSERT INTO public.quizzes_questions(quiz_id, question_id) 
                    values(2,4);
                    INSERT INTO public.quizzes_questions(quiz_id, question_id ) 
                    values(2,5);
                    INSERT INTO public.quizzes_questions(quiz_id, question_id )
                    values(2,6);
                    INSERT INTO public.quizzes_questions(quiz_id, question_id )
                    values(2,1);
        end if;

    end $$

